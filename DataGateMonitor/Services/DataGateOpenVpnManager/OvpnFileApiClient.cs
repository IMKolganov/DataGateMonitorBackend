using System.Net.Http.Headers;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;

namespace DataGateMonitor.Services.DataGateOpenVpnManager;

/// <summary>HTTP client to DataGateOpenVpnManager (<c>api/ovpn-files/*</c>) on the OpenVPN server row.</summary>
public class OvpnFileApiClient(
    IHttpClientFactory httpClientFactory,
    IVpnServerQueryService openVpnServerQueryService,
    IMicroserviceTokenService tokenService,
    ILogger<OvpnFileApiClient> logger)
    : IOvpnFileApiClient
{
    private const string EndpointOvpnFilesAdd = "api/ovpn-files/add";
    private const string EndpointOvpnFilesRevoke = "api/ovpn-files/revoke";
    private const string EndpointOvpnFilesDownload = "api/ovpn-files/download";

    private async Task<HttpClient> GetClientForServer(int serverId, CancellationToken cancellationToken)
    {
        var server = await openVpnServerQueryService.GetById(serverId, cancellationToken)
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

    public async Task<OvpnFileMetadata> AddOvpnFile(int vpnServerId, GenerateOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                "backend", "DataGateOpenVpnManager");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsync(EndpointOvpnFilesAdd, ProjectJson.ToJsonContent(request), cancellationToken);
            await ThrowIfNotSuccessAsync(response, "add OVPN file", cancellationToken);

            var result = await MicroserviceApiResponseHelper.ReadSuccessDataAsync<OvpnFileMetadata>(
                response, cancellationToken);

            logger.LogInformation(
                "Successfully added OVPN file for {CommonName} on server {ServerUrl}, VpnServerId: {VpnServerId}",
                request.CommonName, client.BaseAddress, vpnServerId);

            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while adding OVPN file for {CommonName} on server {VpnServerId}",
                request.CommonName, vpnServerId);
            throw;
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize add OVPN file response for {CommonName} on server {VpnServerId}",
                request.CommonName, vpnServerId);
            throw;
        }
    }

    public async Task<bool> RevokeOvpnFile(int vpnServerId, RevokeOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                "backend", "DataGateOpenVpnManager");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsync(EndpointOvpnFilesRevoke, ProjectJson.ToJsonContent(request), cancellationToken);
            await ThrowIfNotSuccessAsync(response, "revoke OVPN file", cancellationToken);

            var metadata =
                await MicroserviceApiResponseHelper.ReadSuccessDataAsync<OvpnFileMetadata>(response, cancellationToken);

            logger.LogInformation("OVPN file revocation completed for {CommonName} on server {ServerUrl}",
                metadata.CommonName, client.BaseAddress);

            return true;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while revoking OVPN file for {CommonName} on server {VpnServerId}",
                request.CommonName, vpnServerId);
            throw;
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize revoke OVPN file response for {CommonName} on server {VpnServerId}",
                request.CommonName, vpnServerId);
            throw;
        }
    }

    public async Task<OvpnFileDownload> DownloadOvpnFile(int vpnServerId, DownloadOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                "backend", "DataGateOpenVpnManager");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsync(EndpointOvpnFilesDownload, ProjectJson.ToJsonContent(request), cancellationToken);
            await ThrowIfNotSuccessAsync(response, "download OVPN file", cancellationToken);

            var content = await MicroserviceApiResponseHelper.ReadSuccessDataAsync<OvpnFileDownload>(
                response, cancellationToken);

            logger.LogDebug("Successfully downloaded OVPN file {FileName} for {CommonName} on server {ServerUrl}",
                request.FileName, request.CommonName, client.BaseAddress);

            return content;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex,
                "HTTP request failed while downloading OVPN file {FileName} for {CommonName} on server {VpnServerId}",
                request.FileName, request.CommonName, vpnServerId);
            throw;
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize OVPN file content for {FileName} and {CommonName} on server {VpnServerId}",
                request.FileName, request.CommonName, vpnServerId);
            throw;
        }
    }
}
