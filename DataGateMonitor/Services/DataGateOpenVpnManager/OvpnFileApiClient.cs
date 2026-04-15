using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;

namespace DataGateMonitor.Services.DataGateOpenVpnManager;

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
        var server = await openVpnServerQueryService.GetById(serverId, cancellationToken);
        var client = httpClientFactory.CreateClient();
        if (server != null)
        {
            if (string.IsNullOrEmpty(server.ApiUrl))
            {
                throw new InvalidOperationException("API url is missing");
            }
            client.BaseAddress = new Uri(server.ApiUrl);
        }
        else
        {
            throw new Exception("Server not found");
        }
        return client;
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
            
            var response = await client.PostAsJsonAsync(EndpointOvpnFilesAdd, request, cancellationToken);

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

    public async Task<bool> RevokeOvpnFile(int vpnServerId, RevokeOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                "backend", "DataGateOpenVpnManager");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync(EndpointOvpnFilesRevoke, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Service responded with error JSON while revoking OVPN file: {Error}", rawError);

                string extractedError = "Unknown error";
                try
                {
                    var root = JsonConvert.DeserializeObject<JObject>(rawError);
                    extractedError = root?["error"]?.ToString() ?? extractedError;
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
                logger.LogWarning("⚠ Revoke succeeded but metadata is null. CN={CN}, VpnServerId={ID}",
                    request.CommonName, vpnServerId);
            }
            else
            {
                logger.LogInformation("✅ OVPN file revocation completed for {CommonName} on server {ServerUrl}",
                    metadata.CommonName, client.BaseAddress);
            }

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

    public async Task<OvpnFileDownload> DownloadOvpnFile(int vpnServerId, DownloadOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServer(vpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                "backend", "DataGateOpenVpnManager");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            
            var response = await client.PostAsJsonAsync(EndpointOvpnFilesDownload, request, cancellationToken);

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