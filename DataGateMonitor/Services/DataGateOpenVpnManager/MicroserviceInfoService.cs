using System.Net.Http.Headers;
using System.Text.Json;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.DataGateXRayManager.Info;

namespace DataGateMonitor.Services.DataGateOpenVpnManager;

public class MicroserviceInfoService(
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService,
    IVpnServerQueryService openVpnServerQueryService,
    ILogger<MicroserviceInfoService> logger) : IMicroserviceInfoService
{
    private const string EndpointInfo = "api/info";
    private const string AudienceOpenVpnManager = "DataGateOpenVpnManager";
    private const string AudienceXRayManager = "DataGateXRayManager";

    private static readonly JsonSerializerOptions InfoJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<VpnMicroserviceDiagnosticsDto> GetInfoAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        var server = await openVpnServerQueryService.GetById(vpnServerId, cancellationToken)
                     ?? throw new InvalidOperationException($"VPN server not found: {vpnServerId}");

        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException("API URL is not set for the server");

        var audience = server.ServerType == VpnServerType.Xray ? AudienceXRayManager : AudienceOpenVpnManager;

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");
        var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", audience);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var response = await client.GetAsync(EndpointInfo, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var dto = DeserializeDiagnostics(json, server.ServerType);

        logger.LogDebug("Retrieved microservice info for VpnServerId={VpnServerId}, Stack={Stack}",
            vpnServerId, dto.ServerType);

        return dto;
    }

    public async Task<VpnMicroserviceDiagnosticsDto?> GetInfoByUrlAsync(string baseUrl, VpnServerType? serverTypeHint,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));

        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri)
            throw new ArgumentException("Invalid or relative URL.", nameof(baseUrl));

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only HTTP and HTTPS URLs are allowed.", nameof(baseUrl));

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = uri;

        HttpResponseMessage response;
        if (serverTypeHint == VpnServerType.Xray)
        {
            response = await SendInfoRequestAsync(client, AudienceXRayManager, cancellationToken);
        }
        else if (serverTypeHint == VpnServerType.OpenVpn)
        {
            response = await SendInfoRequestAsync(client, AudienceOpenVpnManager, cancellationToken);
        }
        else
        {
            response = await SendInfoRequestAsync(client, AudienceOpenVpnManager, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                response = await SendInfoRequestAsync(client, AudienceXRayManager, cancellationToken);
            }
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogDebug(
                        "Microservice info endpoint not found (404) for {Host}. Server may not be updated yet. Conflog skipped.",
                        uri.Host);
                    return null;
                }

                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var effectiveStack = serverTypeHint ?? InferStackFromApplicationJson(json);
            var dto = DeserializeDiagnostics(json, effectiveStack);

            logger.LogDebug("Retrieved microservice info by URL for {Host}, Stack={Stack}",
                uri.Host, dto.ServerType);

            return dto;
        }
    }

    private Task<HttpResponseMessage> SendInfoRequestAsync(HttpClient client, string audience,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, EndpointInfo);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer",
                tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", audience));
        return client.SendAsync(request, cancellationToken);
    }

    private static VpnMicroserviceDiagnosticsDto DeserializeDiagnostics(string json, VpnServerType stack)
    {
        if (stack == VpnServerType.Xray)
        {
            var xray = JsonSerializer.Deserialize<RootXrayInfoResponse>(json, InfoJsonOptions)
                       ?? throw new InvalidOperationException("Microservice returned empty info response");
            return new VpnMicroserviceDiagnosticsDto { ServerType = VpnServerType.Xray, Xray = xray };
        }

        var openVpn = JsonSerializer.Deserialize<RootOpenVpnInfoResponse>(json, InfoJsonOptions)
                      ?? throw new InvalidOperationException("Microservice returned empty info response");
        return new VpnMicroserviceDiagnosticsDto { ServerType = VpnServerType.OpenVpn, OpenVpn = openVpn };
    }

    private static VpnServerType InferStackFromApplicationJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("application", out var appEl))
                return VpnServerType.OpenVpn;
            var app = appEl.GetString();
            if (string.Equals(app, "DataGateXRayManager", StringComparison.Ordinal))
                return VpnServerType.Xray;
        }
        catch
        {
            // ignored
        }

        return VpnServerType.OpenVpn;
    }
}
