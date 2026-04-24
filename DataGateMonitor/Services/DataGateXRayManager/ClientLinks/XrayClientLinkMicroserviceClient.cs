using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace DataGateMonitor.Services.DataGateXRayManager.ClientLinks;

/// <summary>HTTP client to DataGateXRayManager (<c>api/client-links/*</c>) on the VPN server row.</summary>
public sealed class XrayClientLinkMicroserviceClient(
    IHttpClientFactory httpClientFactory,
    IVpnServerQueryService vpnServerQueryService,
    IMicroserviceTokenService tokenService,
    ILogger<XrayClientLinkMicroserviceClient> logger) : IXrayClientLinkMicroserviceClient
{
    private const string Audience = "DataGateXRayManager";
    private const string EndpointAdd = "api/client-links/add";
    private const string EndpointRevoke = "api/client-links/revoke";
    private const string EndpointDownload = "api/client-links/download";

    private async Task<HttpClient> GetClientForServer(int serverId, CancellationToken cancellationToken)
    {
        var server = await vpnServerQueryService.GetById(serverId, cancellationToken)
                     ?? throw new InvalidOperationException($"VPN server {serverId} not found.");
        if (string.IsNullOrEmpty(server.ApiUrl))
            throw new InvalidOperationException("API url is missing");

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");
        return client;
    }

    public async Task<ClientLinkMetadataDto> AddClientLink(int vpnServerId,
        GenerateClientLinkMicroserviceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", Audience);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync(EndpointAdd, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw await InvalidOpFromErrorBodyAsync(response, "add client link", cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<ClientLinkMetadataDto>(cancellationToken);
            if (result is null)
                throw new InvalidOperationException("Received null response when adding client link.");
            logger.LogInformation("Client link added for {CommonName} on server {Url}, VpnServerId={Id}",
                request.CommonName, client.BaseAddress, vpnServerId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while adding client link for {Cn} on server {Id}",
                request.CommonName, vpnServerId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize add client link response for {Cn} on server {Id}",
                request.CommonName, vpnServerId);
            throw;
        }
    }

    public async Task<ClientLinkMetadataDto> RevokeClientLink(int vpnServerId,
        RevokeClientLinkMicroserviceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", Audience);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync(EndpointRevoke, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw await InvalidOpFromErrorBodyAsync(response, "revoke client link", cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<ClientLinkMetadataDto>(cancellationToken);
            if (result is null)
                throw new InvalidOperationException(
                    $"Revoke client link returned empty body for CommonName={request.CommonName}, VpnServerId={vpnServerId}.");
            logger.LogInformation("Client link revoked for {CommonName} on server {Url}",
                request.CommonName, client.BaseAddress);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while revoking client link for {Cn} on server {Id}",
                request.CommonName, vpnServerId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize revoke client link response for {Cn} on server {Id}",
                request.CommonName, vpnServerId);
            throw;
        }
    }

    public async Task<ClientLinkDownloadDto> DownloadClientLink(int vpnServerId,
        DownloadClientLinkMicroserviceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", Audience);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync(EndpointDownload, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw await InvalidOpFromErrorBodyAsync(response, "download client link", cancellationToken);

            var content = await response.Content.ReadFromJsonAsync<ClientLinkDownloadDto>(cancellationToken);
            if (content is null)
                throw new InvalidOperationException(
                    $"Failed to deserialize client link content for CommonName={request.CommonName}, FileName={request.FileName}.");
            return content;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while downloading client link {File} for {Cn} on server {Id}",
                request.FileName, request.CommonName, vpnServerId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize download client link for {File} on server {Id}",
                request.FileName, vpnServerId);
            throw;
        }
    }

    private static async Task<InvalidOperationException> InvalidOpFromErrorBodyAsync(HttpResponseMessage response,
        string action, CancellationToken cancellationToken)
    {
        var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
        var extracted = "Unknown error";
        try
        {
            var root = JsonConvert.DeserializeObject<JObject>(rawError);
            extracted = root?["error"]?.ToString() ?? extracted;
        }
        catch
        {
            /* ignore */
        }

        return new InvalidOperationException(
            $"Failed to {action}. Status: {(int)response.StatusCode} {response.ReasonPhrase}. Details: {extracted}");
    }
}
