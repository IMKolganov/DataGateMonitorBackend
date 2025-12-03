using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnMicroserviceClientFactory
{
    IOpenVpnMicroserviceClient Create(OpenVpnServer server);
    Task<IOpenVpnMicroserviceClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken);
}