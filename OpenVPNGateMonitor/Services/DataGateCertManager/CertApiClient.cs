using System.Text.Json;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class CertApiClient(
    IHttpClientFactory httpClientFactory,
    IVpnDataService vpnDataService,
    ILogger<CertApiClient> logger)
    : ICertApiClient
{
    private async Task<HttpClient> GetClientForServerAsync(int serverId, CancellationToken cancellationToken)
    {
        var server = await vpnDataService.GetOpenVpnServer(serverId, cancellationToken);
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl);
        return client;
    }

    public async Task<List<CertificateCaInfo>> GetAllCertificatesAsync(int serverId, CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(serverId, cancellationToken);
            var response = await client.GetAsync("api/EasyRsa/GetAllCertificates", cancellationToken);
            response.EnsureSuccessStatusCode();

            var certificates = await response.Content.ReadFromJsonAsync<List<CertificateCaInfo>>(
                cancellationToken: cancellationToken);

            return certificates ?? new List<CertificateCaInfo>();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while getting certificates from server {ServerId}", serverId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize certificates response from server {ServerId}", serverId);
            throw;
        }
    }
    
    public async Task<CertificateBuildResult> BuildCertificateAsync(int serverId, string commonName, CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(serverId, cancellationToken);
            var request = new CertificateBuildRequest
            {
                CommonName = commonName
            };

            var response = await client.PostAsJsonAsync("api/EasyRsa/BuildCertificate", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CertificateBuildResult>(
                cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Received null response when building certificate");
            }

            logger.LogInformation("Successfully built certificate for {CommonName} on server {ServerId}", 
                commonName, serverId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while building certificate for {CommonName} on server {ServerId}", 
                commonName, serverId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize build certificate response for {CommonName} on server {ServerId}", 
                commonName, serverId);
            throw;
        }
    }
    
    public async Task<CertificateRevokeResult> RevokeCertificateAsync(int serverId, string commonName, CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(serverId, cancellationToken);
            var response = await client.PostAsync(
                $"api/EasyRsa/RevokeCertificate/{Uri.EscapeDataString(commonName)}", 
                null, 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CertificateRevokeResult>(
                cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Received null response when revoking certificate");
            }

            logger.LogInformation(
                "Certificate revocation completed for {CommonName} on server {ServerId}. IsRevoked: {IsRevoked}, Message: {Message}", 
                commonName, 
                serverId,
                result.IsRevoked,
                result.Message);

            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while revoking certificate for {CommonName} on server {ServerId}", 
                commonName, serverId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize revoke certificate response for {CommonName} on server {ServerId}", 
                commonName, serverId);
            throw;
        }
    }
    
    public async Task<string> GetPemContentAsync(int serverId, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(serverId, cancellationToken);
            var response = await client.GetAsync(
                $"api/EasyRsa/GetPemContent/{Uri.EscapeDataString(filePath)}", 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<string>(
                cancellationToken: cancellationToken);

            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException($"Empty PEM content received for file: {filePath}");
            }

            logger.LogDebug("Successfully retrieved PEM content from {FilePath} on server {ServerId}", 
                filePath, serverId);
            return content;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while getting PEM content from {FilePath} on server {ServerId}", 
                filePath, serverId);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize PEM content response from {FilePath} on server {ServerId}", 
                filePath, serverId);
            throw;
        }
    }
}