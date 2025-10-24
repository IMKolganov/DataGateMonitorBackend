using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Api.Interfaces;

public interface IVpnDataService
{
    Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken);
    Task<OpenVpnServer> UpdateOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken);
    Task<bool> DeleteOpenVpnServer(int vpnServerId, CancellationToken cancellationToken);
}