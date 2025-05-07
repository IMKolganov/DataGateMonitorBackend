using System.Text.Json;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.Cert.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.Cert.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager;

public class CertApiClient(
    IHttpClientFactory httpClientFactory,
    IVpnDataService vpnDataService,
    ILogger<CertApiClient> logger)
    : ICertApiClient
{
    private static readonly AsyncLocal<bool> _isLogging = new();

    private void LogSafe(Action action)
    {
        if (_isLogging.Value)
            return;

        try
        {
            _isLogging.Value = true;
            action();
        }
        catch
        {
            // Suppress all logging errors
        }
        finally
        {
            _isLogging.Value = false;
        }
    }

    private async Task<HttpClient> GetClientForServerAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        var server = await vpnDataService.GetOpenVpnServer(vpnServerId, cancellationToken);
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl);
        return client;
    }

    public async Task<List<ServerCertificate>> GetAllCertificatesAsync(int vpnServerId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(vpnServerId, cancellationToken);
            var response = await client.GetAsync("api/Cert/GetAllCertificates", cancellationToken);
            response.EnsureSuccessStatusCode();

            var certificates =
                await response.Content.ReadFromJsonAsync<List<ServerCertificate>>(cancellationToken: cancellationToken);
            return certificates ?? new List<ServerCertificate>();
        }
        catch (HttpRequestException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "HTTP request failed (code {Code}) while getting certificates from server {VpnServerId}: {Message}",
                    ex.HResult, vpnServerId, ex.Message));
            throw;
        }
        catch (JsonException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "Deserialization error (code {Code}) while getting certificates from server {VpnServerId}: {Message}",
                    ex.HResult, vpnServerId, ex.Message));
            throw;
        }
    }

    public async Task<ServerCertificate> BuildCertificateAsync(int vpnServerId, string commonName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(vpnServerId, cancellationToken);
            var request = new AddServerCertificateRequest { CommonName = commonName };

            var response = await client.PostAsJsonAsync("api/Cert/AddServerCertificate", request,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<ServerCertificate>(cancellationToken: cancellationToken);
            if (result == null)
                throw new InvalidOperationException("Received null response when building certificate");

            logger.LogInformation("Successfully built certificate for {CommonName} on server {VpnServerId}", commonName,
                vpnServerId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "HTTP request failed (code {Code}) while building certificate for {CommonName}" +
                    " on server {VpnServerId}: {Message}",
                    ex.HResult, commonName, vpnServerId, ex.Message));
            throw;
        }
        catch (JsonException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "Deserialization error (code {Code}) while building certificate for {CommonName}" +
                    " on server {VpnServerId}: {Message}",
                    ex.HResult, commonName, vpnServerId, ex.Message));
            throw;
        }
    }

    public async Task<CertificateRevokeResult> RevokeCertificateAsync(int vpnServerId, string commonName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(vpnServerId, cancellationToken);
            var response = await client.PostAsync($"api/Cert/RevokeCertificate/{Uri.EscapeDataString(commonName)}",
                null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<CertificateRevokeResult>(cancellationToken: cancellationToken);
            if (result == null)
                throw new InvalidOperationException("Received null response when revoking certificate");

            logger.LogInformation(
                "Certificate revocation completed for {CommonName} on server {VpnServerId}." +
                " IsRevoked: {IsRevoked}, Message: {Message}",
                commonName, vpnServerId, result.IsRevoked, result.Message);
            return result;
        }
        catch (HttpRequestException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "HTTP request failed (code {Code}) while revoking certificate for" +
                    " {CommonName} on server {VpnServerId}: {Message}",
                    ex.HResult, commonName, vpnServerId, ex.Message));
            throw;
        }
        catch (JsonException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "Deserialization error (code {Code}) while revoking certificate for {CommonName} " +
                    "on server {VpnServerId}: {Message}",
                    ex.HResult, commonName, vpnServerId, ex.Message));
            throw;
        }
    }
}