using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

public interface IMicroserviceInfoService
{
    /// <summary>Get microservice info by VPN server id (ApiUrl from database).</summary>
    Task<RootInfoResponse> GetInfoAsync(int vpnServerId, CancellationToken cancellationToken);

    /// <summary>Get microservice info by base URL. HTTP and HTTPS allowed (endpoint is Admin/App only). Returns null when endpoint is not available (e.g. 404).</summary>
    Task<RootInfoResponse?> GetInfoByUrlAsync(string baseUrl, CancellationToken cancellationToken);
}
