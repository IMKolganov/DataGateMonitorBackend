using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Api;
using OpenVPNGateMonitor.Models.Helpers.Services;

namespace OpenVPNGateMonitor.Services.Api.Interfaces;

public interface IVpnDataService
{
    Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken);
    Task<OpenVpnServer> UpdateOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken);
    Task<bool> DeleteOpenVpnServer(int vpnServerId, CancellationToken cancellationToken);
}