using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.Services.Others.Notifications.CertApiClient;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Requests;
using DataGateMonitor.SharedModels.Enums;
using AddServerCertificateRequest = DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Requests.AddServerCertificateRequest;

namespace DataGateMonitor.Services.DataGateOpenVpnManager;

/// <summary>Calls <c>api/certs/*</c> on the node agent (<see cref="VpnServerType.OpenVpn"/> → DataGateOpenVpnManager, <see cref="VpnServerType.Xray"/> → DataGateXRayManager).</summary>
public class CertApiClient(
    IHttpClientFactory httpClientFactory,
    IMicroserviceTokenService tokenService,
    IVpnServerQueryService openVpnServerQueryService,
    ICertificateNotificationService certificateNotificationService,
    ILogger<CertApiClient> logger)
    : ICertApiClient
{
    private static readonly AsyncLocal<bool> IsLogging = new();
    private const string EndpointCertsGetAll = "api/certs/get-all";
    private const string EndpointCertsAdd = "api/certs/add";
    private const string EndpointCertsRevoke = "api/certs/revoke";
    private const string AudienceOpenVpnManager = "DataGateOpenVpnManager";
    private const string AudienceXRayManager = "DataGateXRayManager";

    private static async Task<HttpRequestException> BuildManagerFailureExceptionAsync(
        HttpResponseMessage response,
        int vpnServerId,
        VpnServerType serverType,
        CancellationToken cancellationToken)
    {
        var error = await MicroserviceApiResponseHelper.ReadErrorMessageAsync(response, cancellationToken);
        var message = $"Server returned {(int)response.StatusCode} ({response.StatusCode}): {error}";
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var nodeLabel = serverType == VpnServerType.Xray ? "Xray node (DataGateXRayManager)" : "OpenVPN node (DataGateOpenVpnManager)";
            message +=
                $" The {nodeLabel} rejected the JWT issued by this API. "
                + "On the node, set Backend:BaseUrl to this dashboard API base URL so it can fetch MicroserviceJwt public key "
                + $"(vpn server id {vpnServerId} ApiUrl must point to that manager). Restart the manager after key rotation.";
        }

        return new HttpRequestException(message, null, response.StatusCode);
    }

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

    private static string GetCertJwtAudience(VpnServerType serverType) =>
        serverType == VpnServerType.Xray ? AudienceXRayManager : AudienceOpenVpnManager;

    private async Task<(HttpClient Client, VpnServerType ServerType)> GetClientForServerAsync(int vpnServerId,
        CancellationToken cancellationToken)
    {
        var server = await openVpnServerQueryService.GetById(vpnServerId, cancellationToken)
                     ?? throw new InvalidOperationException("VPN server not found");
        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException("API url is missing for the VPN server.");

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(server.ApiUrl.TrimEnd('/') + "/");

        return (client, server.ServerType);
    }

    public async Task<List<ServerCertificate>> GetAllCertificatesAsync(int vpnServerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var (http, serverType) = await GetClientForServerAsync(vpnServerId, cancellationToken);
            using (http)
            {
                var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                    "backend", GetCertJwtAudience(serverType));
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var response = await http.GetAsync(EndpointCertsGetAll, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var ex = await BuildManagerFailureExceptionAsync(response, vpnServerId, serverType,
                        cancellationToken);
                    LogSafe(() => logger.LogError("Failed to get certificates from server {VpnServerId}: {Message}",
                        vpnServerId, ex.Message));
                    throw ex;
                }

                var certificates =
                    await MicroserviceApiResponseHelper.ReadSuccessDataAsync<List<ServerCertificate>>(
                        response, cancellationToken);

                await certificateNotificationService.NotifyReadAllAsync(vpnServerId, certificates.Count,
                    cancellationToken);
                return certificates;
            }
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
            var (http, serverType) = await GetClientForServerAsync(vpnServerId, cancellationToken);
            using (http)
            {
                var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                    "backend", GetCertJwtAudience(serverType));
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var request = new AddServerCertificateRequest { CommonName = commonName };
                var response = await http.PostAsync(EndpointCertsAdd, ProjectJson.ToJsonContent(request),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var ex = await BuildManagerFailureExceptionAsync(response, vpnServerId, serverType,
                        cancellationToken);
                    LogSafe(() =>
                        logger.LogError("Failed to build certificate for {CommonName} on server {VpnServerId}: {Message}",
                            commonName, vpnServerId, ex.Message));
                    throw ex;
                }

                var certificate =
                    await MicroserviceApiResponseHelper.ReadSuccessDataAsync<ServerCertificate>(
                        response, cancellationToken);

                logger.LogInformation("Successfully built certificate for {CommonName} on server {VpnServerId}",
                    commonName,
                    vpnServerId);

                await certificateNotificationService.NotifyBuiltAsync(vpnServerId, certificate, cancellationToken);
                return certificate;
            }
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
            var (http, serverType) = await GetClientForServerAsync(request.VpnServerId, cancellationToken);
            using (http)
            {
                var jwt = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
                    "backend", GetCertJwtAudience(serverType));
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var response = await http.PostAsync(EndpointCertsRevoke, ProjectJson.ToJsonContent(request),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var ex = await BuildManagerFailureExceptionAsync(response, request.VpnServerId, serverType,
                        cancellationToken);
                    LogSafe(() =>
                        logger.LogError(
                            "Failed to revoke certificate for {CommonName} on server {VpnServerId}: {Message}",
                            request.CommonName, request.VpnServerId, ex.Message));
                    throw ex;
                }

                var certificate =
                    await MicroserviceApiResponseHelper.ReadSuccessDataAsync<ServerCertificate>(
                        response, cancellationToken);

                logger.LogInformation(
                    "Certificate revocation completed for {CommonName} on server {VpnServerId}." +
                    " IsRevoked: {IsRevoked}, Message: {Message}",
                    request.CommonName, request.VpnServerId, certificate.IsRevoked, certificate.Message);

                await certificateNotificationService.NotifyRevokedAsync(request.VpnServerId, request, certificate,
                    cancellationToken);
                return certificate;
            }
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
