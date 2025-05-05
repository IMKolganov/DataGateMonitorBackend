using System.Text.Json;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class CertApiClient(HttpClient httpClient, ILogger<CertApiClient> logger) : ICertApiClient
{
    public async Task<List<CertificateCaInfo>> GetAllCertificatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync("api/EasyRsa/GetAllCertificates", cancellationToken);
            response.EnsureSuccessStatusCode();

            var certificates = await response.Content.ReadFromJsonAsync<List<CertificateCaInfo>>(
                cancellationToken: cancellationToken);

            return certificates ?? new List<CertificateCaInfo>();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while getting certificates");
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize certificates response");
            throw;
        }
    }
    
    public async Task<CertificateBuildResult> BuildCertificateAsync(string commonName, CancellationToken cancellationToken)
    {
        try
        {
            var request = new CertificateBuildRequest
            {
                CommonName = commonName
            };

            var response = await httpClient.PostAsJsonAsync("api/EasyRsa/BuildCertificate", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CertificateBuildResult>(
                cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Received null response when building certificate");
            }

            logger.LogInformation("Successfully built certificate for {CommonName}", commonName);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while building certificate for {CommonName}", commonName);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize build certificate response for {CommonName}", commonName);
            throw;
        }
    }
    
    public async Task<CertificateRevokeResult> RevokeCertificateAsync(string commonName, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PostAsync(
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
                "Certificate revocation completed for {CommonName}. IsRevoked: {IsRevoked}, Message: {Message}", 
                commonName, 
                result.IsRevoked,
                result.Message);

            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while revoking certificate for {CommonName}", commonName);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize revoke certificate response for {CommonName}", commonName);
            throw;
        }
    }
    
    public async Task<string> GetPemContentAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"api/EasyRsa/GetPemContent/{Uri.EscapeDataString(filePath)}", 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<string>(
                cancellationToken: cancellationToken);

            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException($"Empty PEM content received for file: {filePath}");
            }

            logger.LogDebug("Successfully retrieved PEM content from {FilePath}", filePath);
            return content;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while getting PEM content from {FilePath}", filePath);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize PEM content response from {FilePath}", filePath);
            throw;
        }
    }
}