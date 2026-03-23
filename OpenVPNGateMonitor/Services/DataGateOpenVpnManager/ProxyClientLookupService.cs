using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy.Responses;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager;

/// <summary>
/// Calls <c>GET api/proxy/client/by-local-port</c> on the per-server microservice (same base URL and JWT as other HTTP integrations).
/// </summary>
public sealed class ProxyClientLookupService(
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService,
    IGeoLiteQueryService geoLiteQueryService,
    ILogger<ProxyClientLookupService> logger) : IProxyClientLookupService
{
    /// <summary>Relative to microservice base (trailing slash added by caller).</summary>
    private const string ProxyClientByLocalPortPath = "api/proxy/client/by-local-port";

    public async Task EnrichFromManagementRealAddressAsync(OpenVpnServer server, OpenVpnServerClient client,
        CancellationToken ct)
    {
        var lookup = await TryLookupAsync(server, client.RemoteIp, ct).ConfigureAwait(false);
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

    private async Task<ProxyClientLookupResponse?> TryLookupAsync(OpenVpnServer server, string realAddress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            return null;

        if (!TryParseLoopbackIpAndPort(realAddress, out var host, out var localPort))
            return null;

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
                logger.LogDebug(
                    "Proxy client lookup returned 404 for VpnServerId={ServerId}, {Host}:{Port}",
                    server.Id, host, localPort);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<ProxyClientLookupResponse>(cancellationToken: ct)
                       .ConfigureAwait(false);

            return body;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Proxy client lookup failed for VpnServerId={ServerId}, RealAddress={RealAddress}",
                server.Id, realAddress);
            return null;
        }
    }

    private async Task ApplyGeoFromIpAsync(OpenVpnServerClient client, string ip, CancellationToken ct)
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
