using System.Text.Json;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class OvpnFileApiClient(HttpClient httpClient, ILogger<OvpnFileApiClient> logger) : IOvpnFileApiClient
{
    public async Task<IssuedOvpnFile> AddOvpnFileAsync(AddOvpnFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/OvpnFile/AddOvpnFile", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<IssuedOvpnFile>(cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Received null response when adding OVPN file");
            }

            logger.LogInformation("Successfully added OVPN file for {CommonName}", request.CommonName);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while adding OVPN file for {CommonName}", request.CommonName);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize add OVPN file response for {CommonName}", request.CommonName);
            throw;
        }
    }

    public async Task<bool> RevokeOvpnFileAsync(RevokeOvpnFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/OvpnFile/RevokeOvpnFile", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<bool>(cancellationToken: cancellationToken);
            
            logger.LogInformation("OVPN file revocation completed for {CommonName}", request.CommonName);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while revoking OVPN file for {CommonName}", request.CommonName);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize revoke OVPN file response for {CommonName}", request.CommonName);
            throw;
        }
    }

    public async Task<string> DownloadOvpnFileAsync(DownloadOvpnFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/OvpnFile/DownloadOvpnFile", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<string>(cancellationToken: cancellationToken);

            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException($"Empty OVPN content received for file: {request.FileName}");
            }

            logger.LogDebug("Successfully downloaded OVPN file {FileName} for {CommonName}", 
                request.FileName, request.CommonName);
            return content;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while downloading OVPN file {FileName} for {CommonName}", 
                request.FileName, request.CommonName);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize OVPN file content for {FileName} and {CommonName}", 
                request.FileName, request.CommonName);
            throw;
        }
    }
}