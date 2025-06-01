using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;

public interface IOpenVpnServerService
{
    Task SaveConnectedClientsAsync(OpenVpnServer openVpnServer, CancellationToken cancellationToken);
    Task SaveOpenVpnServerStatusLogAsync(OpenVpnServer openVpnServer, CancellationToken cancellationToken);
}