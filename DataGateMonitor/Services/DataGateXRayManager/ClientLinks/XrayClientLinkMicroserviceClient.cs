using System.Net.Http.Headers;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Helpers;

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

    private static async Task ThrowIfNotSuccessAsync(HttpResponseMessage response, string action,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var detail = await MicroserviceApiResponseHelper.ReadErrorMessageAsync(response, cancellationToken);
        throw new InvalidOperationException(
            $"Failed to {action}. Status: {(int)response.StatusCode} {response.ReasonPhrase}. Details: {detail}");
    }

    public async Task<ClientLinkMetadataDto> AddClientLink(int vpnServerId,
        GenerateClientLinkMicroserviceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", Audience);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsync(EndpointAdd, ProjectJson.ToJsonContent(request), cancellationToken);
            await ThrowIfNotSuccessAsync(response, "add client link", cancellationToken);

            var result = await MicroserviceApiResponseHelper.ReadSuccessDataAsync<ClientLinkMetadataDto>(
                response, cancellationToken);
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
        catch (Newtonsoft.Json.JsonException ex)
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

            var response = await client.PostAsync(EndpointRevoke, ProjectJson.ToJsonContent(request), cancellationToken);
            await ThrowIfNotSuccessAsync(response, "revoke client link", cancellationToken);

            var result = await MicroserviceApiResponseHelper.ReadSuccessDataAsync<ClientLinkMetadataDto>(
                response, cancellationToken);
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
        catch (Newtonsoft.Json.JsonException ex)
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

            var response = await client.PostAsync(EndpointDownload, ProjectJson.ToJsonContent(request), cancellationToken);
            await ThrowIfNotSuccessAsync(response, "download client link", cancellationToken);

            return await MicroserviceApiResponseHelper.ReadSuccessDataAsync<ClientLinkDownloadDto>(
                response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while downloading client link {File} for {Cn} on server {Id}",
                request.FileName, request.CommonName, vpnServerId);
            throw;
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize download client link for {File} on server {Id}",
                request.FileName, vpnServerId);
            throw;
        }
    }
}
