using DataGateMonitor.Models;

namespace DataGateMonitor.Services.BackgroundServices.Interfaces;

/// <summary>
/// Polls a single VPN/proxy node (OpenVPN, Xray, …) and updates DB state.
/// </summary>
public interface IVpnServerWorkProcessor
{
    Task ProcessServerAsync(VpnServer server, CancellationToken cancellationToken);
}
