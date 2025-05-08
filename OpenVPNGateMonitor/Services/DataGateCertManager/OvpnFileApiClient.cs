using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var response = await client.PostAsJsonAsync("api/OvpnFile/AddOvpnFile", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Service responded with error JSON: {Error}", rawError);

                string extractedError = "Unknown error";

                try
                {
                    var root = JsonConvert.DeserializeObject<JObject>(rawError);
                    var error = root?["error"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(error))
                        extractedError = error;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse error JSON from service response.");
                }

                throw new InvalidOperationException(
                    $"Failed to add OVPN file. Status: {(int)response.StatusCode} {response.ReasonPhrase}. Details:\n{extractedError}");
            }

            var result = await response.Content.ReadFromJsonAsync<OvpnFileMetadata>(cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Received null response when adding OVPN file.");
            }

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
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize add OVPN file response for {CommonName} on server {VpnServerId}",
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

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Service responded with error JSON while revoking OVPN file: {Error}", rawError);

                string extractedError = "Unknown error";

                try
                {
                    var root = JsonConvert.DeserializeObject<JObject>(rawError);
                    var error = root?["error"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(error))
                        extractedError = error;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse JSON error from revoke response.");
                }

                throw new InvalidOperationException(
                    $"Failed to revoke OVPN file. Status: {(int)response.StatusCode} {response.ReasonPhrase}. Details: {extractedError}");
            }

            var metadata =
                await response.Content.ReadFromJsonAsync<OvpnFileMetadata>(cancellationToken: cancellationToken);

            if (metadata == null)
            {
                logger.LogWarning(
                    "Received null metadata after revoking OVPN file for {CommonName} on server {ServerUrl}",
                    request.CommonName, client.BaseAddress);
                return false;
            }

            logger.LogInformation("OVPN file revocation completed for {CommonName} on server {ServerUrl}",
                request.CommonName, client.BaseAddress);

            return true;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while revoking OVPN file for {CommonName} on server {VpnServerId}",
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
            var response = await client.PostAsJsonAsync("api/OvpnFile/DownloadOvpnFile", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Service responded with error JSON: {Error}", rawError);

                string extractedError = "Unknown error";

                try
                {
                    var root = JsonConvert.DeserializeObject<JObject>(rawError);
                    var error = root?["error"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(error))
                        extractedError = error;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse simple JSON error from service.");
                }

                throw new InvalidOperationException(
                    $"Failed to download OVPN file. Status: {(int)response.StatusCode} {response.ReasonPhrase}. Details: {extractedError}");
            }

            var content = await response.Content.ReadFromJsonAsync<OvpnFileDownload>(cancellationToken);

            if (content == null)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize OVPN file content for CommonName: {request.CommonName}, FileName: {request.FileName}. The server returned a successful status code but the content was null.");
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