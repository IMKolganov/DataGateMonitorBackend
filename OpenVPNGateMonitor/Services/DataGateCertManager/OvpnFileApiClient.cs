using Newtonsoft.Json;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class OvpnFileApiClient(
    IHttpClientFactory httpClientFactory,
    IVpnDataService vpnDataService,
    ILogger<OvpnFileApiClient> logger)
    : IOvpnFileApiClient
{
    private async Task<HttpClient> GetClientForServerAsync(int serverId, CancellationToken cancellationToken)
    {
        var server = await vpnDataService.GetOpenVpnServer(serverId, cancellationToken);
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl);
        return client;
    }

    public async Task<OvpnFileMetadata> AddOvpnFileAsync(int vpnServerId, AddOvpnFileRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(vpnServerId, cancellationToken);
            var response = await client.PostAsJsonAsync("api/OvpnFile/AddOvpnFile", request, 
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OvpnFileMetadata>(cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Received null response when adding OVPN file");
            }

            logger.LogInformation("Successfully added OVPN file for " +
                                  "{CommonName} on server {ServerUrl} VpnServerId: {VpnServerId}",
                request.CommonName, client.BaseAddress, vpnServerId);
            
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while adding OVPN file for " +
                                "{CommonName} on server {VpnServerId}",
                request.CommonName, vpnServerId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize add OVPN file response for " +
                                "{CommonName} on server {VpnServerId}",
                request.CommonName, vpnServerId);
            throw;
        }
    }

    public async Task<bool> RevokeOvpnFileAsync(int vpnServerId, RevokeOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(vpnServerId, cancellationToken);
            var response = await client.PostAsJsonAsync("api/OvpnFile/RevokeOvpnFile", request,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<bool>(cancellationToken: cancellationToken);

            logger.LogInformation("OVPN file revocation completed for {CommonName} on server {ServerUrl}",
                request.CommonName, client.BaseAddress);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while revoking OVPN file for " +
                                "{CommonName} on server {VpnServerId}",
                request.CommonName, vpnServerId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize revoke OVPN file response for {CommonName} on server {VpnServerId}",
                request.CommonName, vpnServerId);
            throw;
        }
    }

    public async Task<OvpnFileDownload> DownloadOvpnFileAsync(int vpnServerId, DownloadOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(vpnServerId, cancellationToken);
            var response = await client.PostAsJsonAsync("api/OvpnFile/DownloadOvpnFile", request, 
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<OvpnFileDownload>(cancellationToken);
            
            logger.LogDebug("Successfully downloaded OVPN file {FileName} for {CommonName} on server {ServerUrl}",
                request.FileName, request.CommonName, client.BaseAddress);
            return content ?? 
                   throw new InvalidOperationException($"Failed to deserialize OVPN file content for CommonName:" +
                                                       $" {request.CommonName}, FileName: {request.FileName}." +
                                                       $" The server returned a successful status " +
                                                       $"code but the content was null.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex,
                "HTTP request failed while downloading OVPN file {FileName} for {CommonName} " +
                "on server {VpnServerId}",
                request.FileName, request.CommonName, vpnServerId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize OVPN file content for {FileName} and {CommonName} " +
                "on server {VpnServerId}",
                request.FileName, request.CommonName, vpnServerId);
            throw;
        }
    }
}