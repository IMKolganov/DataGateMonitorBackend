using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy.Responses;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Services.DataGateOpenVpnManager;

/// <summary>
/// Calls <c>GET api/proxy/client/by-local-port</c> on the per-server microservice (same base URL and JWT as other HTTP integrations).
/// </summary>
public sealed class ProxyClientLookupService(
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService,
    IGeoLiteQueryService geoLiteQueryService,
    IOpenVpnMicroserviceNotificationService microserviceNotifications,
    ILogger<ProxyClientLookupService> logger) : IProxyClientLookupService
{
    /// <summary>Relative to microservice base (trailing slash added by caller).</summary>
    private const string ProxyClientByLocalPortPath = "api/proxy/client/by-local-port";

    /// <summary>At most one admin notification per server within this window (many clients can connect at once).</summary>
    private static readonly TimeSpan ProxyLookupNotifyThrottle = TimeSpan.FromMinutes(10);

    private static readonly ConcurrentDictionary<int, DateTimeOffset> s_lastProxyLookupNotifyUtc = new();

    public async Task EnrichFromManagementRealAddressAsync(VpnServer server, VpnServerClient client,
        CancellationToken ct)
    {
        var lookup = await TryLookupAsync(server, client, ct).ConfigureAwait(false);
        if (lookup is null)
        {
            client.ProxyRealIp = null;
            await ApplyGeoFromIpAsync(client, client.RemoteIp, ct).ConfigureAwait(false);
            return;
        }

        client.ProxyRealIp = FormatProxyRealIpValue(lookup);
        var ipForGeo = string.IsNullOrWhiteSpace(lookup.RealClientIp) ? client.RemoteIp : lookup.RealClientIp;
        await ApplyGeoFromIpAsync(client, ipForGeo, ct).ConfigureAwait(false);
    }

    /// <summary>Parses management <c>RealAddress</c> when it is loopback IP + port (e.g. <c>127.0.0.1:41810</c>).</summary>
    public static bool TryParseLoopbackIpAndPort(string realAddress, out string host, out int localPort)
    {
        host = string.Empty;
        localPort = 0;

        if (string.IsNullOrWhiteSpace(realAddress))
            return false;

        if (!IPEndPoint.TryParse(realAddress.Trim(), out var ep))
            return false;

        if (!IPAddress.IsLoopback(ep.Address))
            return false;

        localPort = ep.Port;
        if (localPort is < 1 or > 65535)
            return false;

        host = ep.Address.ToString();

        return true;
    }

    public static string? FormatProxyRealIpValue(ProxyClientLookupResponse r)
    {
        if (string.IsNullOrWhiteSpace(r.RealClientIp))
            return null;

        if (r.RealClientPort > 0)
            return $"{r.RealClientIp}:{r.RealClientPort}";

        return r.RealClientIp;
    }

    private async Task<ProxyClientLookupResponse?> TryLookupAsync(VpnServer server, VpnServerClient client,
        CancellationToken ct)
    {
        var realAddress = client.RemoteIp;

        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            return null;

        if (!TryParseLoopbackIpAndPort(realAddress, out var host, out var localPort))
            return null;

        var lookupUrl = BuildLookupUrl(server.ApiUrl, localPort, host);

        try
        {
            using var http = httpClientFactory.CreateClient();
            http.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", "DataGateOpenVpnManager");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var url =
                $"{ProxyClientByLocalPortPath}?localPort={localPort.ToString(CultureInfo.InvariantCulture)}&host={Uri.EscapeDataString(host)}";

            var response = await http.GetAsync(url, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var responseMessage = await TryReadApiErrorMessageAsync(response.Content, ct).ConfigureAwait(false);
                var outcome = FormatHttpOutcome(response.StatusCode, responseMessage);
                logger.LogWarning(
                    "Proxy client lookup returned 404 for VpnServerId={ServerId}, CommonName={CommonName}, LookupUrl={LookupUrl}",
                    server.Id, client.CommonName, lookupUrl);
                await NotifyLookupFailureAsync(server, client, realAddress, lookupUrl, outcome,
                    NotificationSeverity.Warning, ct).ConfigureAwait(false);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var responseMessage = await TryReadApiErrorMessageAsync(response.Content, ct).ConfigureAwait(false);
                var outcome = FormatHttpOutcome(response.StatusCode, responseMessage);
                logger.LogWarning(
                    "Proxy client lookup returned {StatusCode} for VpnServerId={ServerId}, CommonName={CommonName}, LookupUrl={LookupUrl}",
                    (int)response.StatusCode, server.Id, client.CommonName, lookupUrl);
                await NotifyLookupFailureAsync(server, client, realAddress, lookupUrl, outcome,
                    NotificationSeverity.Error, ct).ConfigureAwait(false);
                return null;
            }

            var wrapped = await ProjectJson.ReadContentAsync<ApiResponse<ProxyClientLookupResponse>>(response.Content, ct)
                .ConfigureAwait(false);

            if (wrapped is not { Success: true, Data: not null })
            {
                var detail = wrapped?.Message is { Length: > 0 } msg
                    ? msg
                    : "Empty or invalid JSON in proxy client lookup response";
                await NotifyLookupFailureAsync(server, client, realAddress, lookupUrl, detail,
                    NotificationSeverity.Error, ct).ConfigureAwait(false);
                return null;
            }

            return wrapped.Data;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Proxy client lookup failed for VpnServerId={ServerId}, CommonName={CommonName}, RealAddress={RealAddress}, LookupUrl={LookupUrl}",
                server.Id, client.CommonName, realAddress, lookupUrl);
            await NotifyLookupFailureAsync(server, client, realAddress, lookupUrl,
                $"{ex.GetType().Name}: {ex.Message}", NotificationSeverity.Error, ct).ConfigureAwait(false);
            return null;
        }
    }

    private async Task NotifyLookupFailureAsync(VpnServer server, VpnServerClient client, string realAddress,
        string lookupUrl, string outcomeDetail, NotificationSeverity severity, CancellationToken ct)
    {
        if (!TryAcquireProxyLookupNotifySlot(server.Id))
            return;

        try
        {
            await microserviceNotifications.NotifyProxyClientLookupFailed(
                server.Id,
                server.ServerName,
                BuildLookupFailureDetail(server, client, realAddress, lookupUrl, outcomeDetail),
                severity,
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to send proxy client lookup notification for VpnServerId={ServerId}",
                server.Id);
        }
    }

    private static string BuildLookupUrl(string apiUrl, int localPort, string host) =>
        $"{apiUrl.TrimEnd('/')}/{ProxyClientByLocalPortPath}?localPort={localPort.ToString(CultureInfo.InvariantCulture)}&host={Uri.EscapeDataString(host)}";

    private static string BuildLookupFailureDetail(VpnServer server, VpnServerClient client, string realAddress,
        string lookupUrl, string outcomeDetail)
    {
        var parts = new List<string> { $"RealAddress={realAddress}" };

        if (!string.IsNullOrWhiteSpace(client.CommonName))
            parts.Add($"CommonName={client.CommonName}");
        if (!string.IsNullOrWhiteSpace(client.LocalIp))
            parts.Add($"VirtualAddress={client.LocalIp}");
        if (client.ConnectedSince > DateTimeOffset.MinValue)
            parts.Add($"ConnectedSince={client.ConnectedSince:O}");
        if (!string.IsNullOrWhiteSpace(client.ExternalId))
            parts.Add($"ExternalId={client.ExternalId}");
        if (!string.IsNullOrWhiteSpace(client.Username)
            && !string.Equals(client.Username, client.CommonName, StringComparison.Ordinal))
            parts.Add($"Username={client.Username}");
        if (!string.IsNullOrWhiteSpace(server.ApiUrl))
            parts.Add($"ApiUrl={server.ApiUrl.TrimEnd('/')}");
        parts.Add($"LookupUrl={lookupUrl}");
        parts.Add(outcomeDetail);

        return string.Join("; ", parts);
    }

    private static string FormatHttpOutcome(HttpStatusCode statusCode, string? responseMessage)
    {
        var outcome = $"HTTP {(int)statusCode} {statusCode}";
        return string.IsNullOrWhiteSpace(responseMessage)
            ? outcome
            : $"{outcome}; Response={responseMessage}";
    }

    private static async Task<string?> TryReadApiErrorMessageAsync(HttpContent content, CancellationToken ct)
    {
        try
        {
            var wrapped = await ProjectJson.ReadContentAsync<ApiResponse<object?>>(content, ct).ConfigureAwait(false);
            return wrapped?.Message is { Length: > 0 } msg ? msg : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryAcquireProxyLookupNotifySlot(int serverId)
    {
        var now = DateTimeOffset.UtcNow;
        var shouldNotify = s_lastProxyLookupNotifyUtc.AddOrUpdate(
            serverId,
            _ => now,
            (_, last) => now - last >= ProxyLookupNotifyThrottle ? now : last);

        return shouldNotify == now;
    }

    private async Task ApplyGeoFromIpAsync(VpnServerClient client, string ip, CancellationToken ct)
    {
        var geoInfo = await geoLiteQueryService.GetGeoInfoAsync(ip, ct).ConfigureAwait(false);
        if (geoInfo is null)
            return;

        client.Country = geoInfo.Country;
        client.Region = geoInfo.Region;
        client.City = geoInfo.City;
        client.Latitude = geoInfo.Latitude;
        client.Longitude = geoInfo.Longitude;
    }
}
