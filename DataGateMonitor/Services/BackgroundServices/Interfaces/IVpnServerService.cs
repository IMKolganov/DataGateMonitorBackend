using DataGateMonitor.Models;

namespace DataGateMonitor.Services.BackgroundServices.Interfaces;

public interface IVpnServerService
{
    Task SaveConnectedClientsAsync(VpnServer openVpnServer, CancellationToken cancellationToken);
    Task SaveVpnServerStatusLogAsync(VpnServer openVpnServer, CancellationToken cancellationToken);
}