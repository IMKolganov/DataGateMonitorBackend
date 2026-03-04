using System.Net.Http.Headers;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Info;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager;

public class MicroserviceInfoService(
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService,
    IOpenVpnServerQueryService openVpnServerQueryService,
    ILogger<MicroserviceInfoService> logger) : IMicroserviceInfoService
{
    private const string EndpointInfo = "api/info";

    public async Task<RootInfoResponse> GetInfoAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        var server = await openVpnServerQueryService.GetById(vpnServerId, cancellationToken)
                     ?? throw new InvalidOperationException($"OpenVPN server not found: {vpnServerId}");

        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException("API URL is not set for the server");

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");
        var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", "DataGateOpenVpnManager");
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
        var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", "DataGateOpenVpnManager");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync(EndpointInfo, cancellationToken);
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
