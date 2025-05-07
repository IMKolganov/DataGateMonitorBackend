using System.Text.Json;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Requests;

namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class OvpnFileApiClient(
    IHttpClientFactory httpClientFactory,
    IVpnDataService vpnDataService,
    IUnitOfWork unitOfWork,
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

    public async Task<IssuedOvpnFile> AddOvpnFileAsync(int vpnServerId, AddOvpnFileRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(vpnServerId, cancellationToken);
            var response = await client.PostAsJsonAsync("api/OvpnFile/AddOvpnFile", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<IssuedOvpnFile>(cancellationToken: cancellationToken);

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
            var response = await client.PostAsJsonAsync("api/OvpnFile/RevokeOvpnFile", request, cancellationToken);
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

    public async Task<string> DownloadOvpnFileAsync(int vpnServerId, DownloadOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(vpnServerId, cancellationToken);
            var response = await client.PostAsJsonAsync("api/OvpnFile/DownloadOvpnFile", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<string>(cancellationToken: cancellationToken);

            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException($"Empty OVPN content received for file: {request.FileName}");
            }

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
        catch (JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize OVPN file content for {FileName} and {CommonName} on server {VpnServerId}",
                request.FileName, request.CommonName, vpnServerId);
            throw;
        }
    }
}