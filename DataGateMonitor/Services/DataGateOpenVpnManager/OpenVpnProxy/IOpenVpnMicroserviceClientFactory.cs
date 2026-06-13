using DataGateMonitor.Models;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnMicroserviceClientFactory
{
    IOpenVpnMicroserviceClient Create(VpnServer server);
    Task<IOpenVpnMicroserviceClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken);

    /// <summary>Removes cached client for server so next use gets a fresh one (e.g. after server ApiUrl update).</summary>
    void Invalidate(int serverId);
}