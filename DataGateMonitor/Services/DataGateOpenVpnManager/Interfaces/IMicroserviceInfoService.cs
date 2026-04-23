using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

public interface IMicroserviceInfoService
{
    /// <summary>Get microservice info by VPN server id (ApiUrl from database).</summary>
    Task<VpnMicroserviceDiagnosticsDto> GetInfoAsync(int vpnServerId, CancellationToken cancellationToken);

    /// <summary>
    /// Get microservice info by base URL. HTTP and HTTPS allowed (endpoint is Admin/App only).
    /// When <paramref name="serverTypeHint"/> is set, uses the matching JWT audience first.
    /// Returns null when endpoint is not available (e.g. 404).
    /// </summary>
    Task<VpnMicroserviceDiagnosticsDto?> GetInfoByUrlAsync(string baseUrl, VpnServerType? serverTypeHint,
        CancellationToken cancellationToken);
}
