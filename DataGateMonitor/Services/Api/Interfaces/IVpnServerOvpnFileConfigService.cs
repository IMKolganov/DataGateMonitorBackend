using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Api.Interfaces;

public interface IVpnServerOvpnFileConfigService
{
    Task<VpnServerOvpnFileConfig> GetVpnServerOvpnFileConfigByServerId(int vpnServerId, 
        CancellationToken cancellationToken);

    Task<VpnServerOvpnFileConfig> AddOrUpdateVpnServerOvpnFileConfigByServerId(
        VpnServerOvpnFileConfig openVpnServerOvpnFileConfig, CancellationToken cancellationToken);
}