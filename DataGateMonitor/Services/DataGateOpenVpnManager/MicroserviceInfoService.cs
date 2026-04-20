using System.Net.Http.Headers;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.Enums;

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

    public async Task<RootInfoResponse> GetInfoAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        var server = await openVpnServerQueryService.GetById(vpnServerId, cancellationToken)
                     ?? throw new InvalidOperationException($"OpenVPN server not found: {vpnServerId}");

        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException("API URL is not set for the server");

        var audience = server.ServerType == VpnServerType.Xray ? AudienceXRayManager : AudienceOpenVpnManager;

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");
        var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", audience);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync(EndpointInfo, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RootInfoResponse>(cancellationToken)
                     ?? throw new InvalidOperationException("Microservice returned empty info response");

        logger.LogDebug("Retrieved microservice info for VpnServerId={VpnServerId}, Version={Version}",
            vpnServerId, result.Version);

        return result;
    }

    public async Task<RootInfoResponse?> GetInfoByUrlAsync(string baseUrl, CancellationToken cancellationToken)
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

        using var request = new HttpRequestMessage(HttpMethod.Get, EndpointInfo);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer",
                tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", AudienceOpenVpnManager));

        var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            using var retry = new HttpRequestMessage(HttpMethod.Get, EndpointInfo);
            retry.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", AudienceXRayManager));
            response.Dispose();
            response = await client.SendAsync(retry, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogDebug(
                    "Microservice info endpoint not found (404) for {Host}. Server may not be updated yet. Conflog skipped.",
                    uri.Host);
                return (RootInfoResponse?)null;
            }

            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<RootInfoResponse>(cancellationToken)
                     ?? throw new InvalidOperationException("Microservice returned empty info response");

        logger.LogDebug("Retrieved microservice info by URL for {Host}, Version={Version}",
            uri.Host, result.Version);

        return result;
    }
}
