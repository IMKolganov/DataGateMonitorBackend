using System.Net.Http.Headers;
using System.Text.Json;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.Services.Others.Notifications.CertApiClient;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Requests;
using AddServerCertificateRequest = OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Requests.AddServerCertificateRequest;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager;

public class CertApiClient(
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService,
    IOpenVpnServerQueryService openVpnServerQueryService,
    ICertificateNotificationService certificateNotificationService,
    ILogger<CertApiClient> logger)
    : ICertApiClient
{
    private static readonly AsyncLocal<bool> IsLogging = new();
    private const string EndpointCertsGetAll = "api/certs/get-all";
    private const string EndpointCertsAdd = "api/certs/add";
    private const string EndpointCertsRevoke = "api/certs/revoke";
    
    private void LogSafe(Action action)
    {
        if (IsLogging.Value)
            return;

        try
        {
            IsLogging.Value = true;
            action();
        }
        catch
        {
            // Suppress all logging errors
        }
        finally
        {
            IsLogging.Value = false;
        }
    }

    private async Task<HttpClient> GetClientForServerAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        var server = await openVpnServerQueryService.GetById(vpnServerId, cancellationToken) 
            ?? throw new InvalidOperationException("OpenVPN server not found");
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
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
            "backend", "DataGateOpenVpnManager");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync(EndpointCertsGetAll, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var message = $"Server returned {(int)response.StatusCode} ({response.StatusCode}): {error}";
                LogSafe(() => logger.LogError("Failed to get certificates from server {VpnServerId}: {Message}",
                    vpnServerId, message));
                throw new HttpRequestException(message, null, response.StatusCode);
            }

            var certificates =
                await response.Content.ReadFromJsonAsync<List<ServerCertificate>>(cancellationToken: cancellationToken);
            
            await certificateNotificationService.NotifyReadAllAsync(vpnServerId, certificates!.Count, cancellationToken);
            return certificates;
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
                    "Deserialization error (code {Code}) while getting certificates " +
                    "from server {VpnServerId}: {Message}",
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
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                "backend", "DataGateOpenVpnManager");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var request = new AddServerCertificateRequest { CommonName = commonName };
            var response = await client.PostAsJsonAsync(EndpointCertsAdd, request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var message = $"Server returned {(int)response.StatusCode} ({response.StatusCode}): {error}";
                LogSafe(() =>
                    logger.LogError("Failed to build certificate for {CommonName} on server {VpnServerId}: {Message}",
                        commonName, vpnServerId, message));
                throw new HttpRequestException(message, null, response.StatusCode);
            }

            var certificate =
                await response.Content.ReadFromJsonAsync<ServerCertificate>(cancellationToken: cancellationToken);
            if (certificate == null)
                throw new InvalidOperationException("Received null response when building certificate");

            logger.LogInformation("Successfully built certificate for {CommonName} on server {VpnServerId}", commonName,
                vpnServerId);

            await certificateNotificationService.NotifyBuiltAsync(vpnServerId, certificate, cancellationToken);
            return certificate;
        }
        catch (HttpRequestException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "HTTP request failed (code {Code}) while building certificate for {CommonName} " +
                    "on server {VpnServerId}: {Message}",
                    ex.HResult, commonName, vpnServerId, ex.Message));
            throw;
        }
        catch (JsonException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "Deserialization error (code {Code}) while building certificate for {CommonName} " +
                    "on server {VpnServerId}: {Message}",
                    ex.HResult, commonName, vpnServerId, ex.Message));
            throw;
        }
    }

    public async Task<ServerCertificate> RevokeCertificateAsync(RevokeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await GetClientForServerAsync(request.VpnServerId, cancellationToken);
            var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                "backend", "DataGateOpenVpnManager");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsJsonAsync(EndpointCertsRevoke, request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var message = $"Server returned {(int)response.StatusCode} ({response.StatusCode}): {error}";
                LogSafe(() =>
                    logger.LogError("Failed to revoke certificate for {CommonName} on server {VpnServerId}: {Message}",
                        request.CommonName, request.VpnServerId, message));
                throw new HttpRequestException(message, null, response.StatusCode);
            }

            var certificate =
                await response.Content.ReadFromJsonAsync<ServerCertificate>(cancellationToken: cancellationToken);
            if (certificate == null)
                throw new InvalidOperationException("Received null response when revoking certificate");

            logger.LogInformation(
                "Certificate revocation completed for {CommonName} on server {VpnServerId}." +
                " IsRevoked: {IsRevoked}, Message: {Message}",
                request.CommonName, request.VpnServerId, certificate.IsRevoked, certificate.Message);
            
            await certificateNotificationService.NotifyRevokedAsync(request.VpnServerId, request, certificate, 
                cancellationToken);
            return certificate;
        }
        catch (HttpRequestException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "HTTP request failed (code {Code}) while revoking certificate for {CommonName} " +
                    "on server {VpnServerId}: {Message}",
                    ex.HResult, request.CommonName, request.VpnServerId, ex.Message));
            throw;
        }
        catch (JsonException ex)
        {
            LogSafe(() =>
                logger.LogError(
                    "Deserialization error (code {Code}) while revoking certificate for {CommonName} " +
                    "on server {VpnServerId}: {Message}",
                    ex.HResult, request.CommonName, request.VpnServerId, ex.Message));
            throw;
        }
    }
}