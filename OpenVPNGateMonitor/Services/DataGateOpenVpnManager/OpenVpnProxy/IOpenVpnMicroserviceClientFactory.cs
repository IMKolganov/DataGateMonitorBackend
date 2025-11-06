using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnMicroserviceClientFactory
{
    OpenVpnMicroserviceClient Create(OpenVpnServer server);
    Task<OpenVpnMicroserviceClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken);
}