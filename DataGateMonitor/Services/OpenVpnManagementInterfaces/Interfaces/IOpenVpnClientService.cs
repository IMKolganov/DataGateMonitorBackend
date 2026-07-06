using DataGateMonitor.Models;

namespace DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

public interface IOpenVpnClientService
{
    Task<OpenVpnManagementStatusResult> GetClientsFromManagementAsync(VpnServer openVpnServer,
        CancellationToken cancellationToken);

    /// <summary>
    /// Disconnect a connected client via OpenVPN management (<c>client-kill CID</c> or <c>kill CN</c>).
    /// </summary>
    Task KillConnectedClientAsync(VpnServer openVpnServer, VpnServerClient client, CancellationToken cancellationToken);
}