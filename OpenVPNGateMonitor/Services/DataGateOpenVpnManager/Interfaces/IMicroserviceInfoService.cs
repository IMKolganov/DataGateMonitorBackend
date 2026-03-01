using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Info;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

public interface IMicroserviceInfoService
{
    /// <summary>Get microservice info by VPN server id (ApiUrl from database).</summary>
    Task<RootInfoResponse> GetInfoAsync(int vpnServerId, CancellationToken cancellationToken);

    /// <summary>Get microservice info by base URL. HTTP and HTTPS allowed (endpoint is Admin/App only).</summary>
    Task<RootInfoResponse> GetInfoByUrlAsync(string baseUrl, CancellationToken cancellationToken);
}
