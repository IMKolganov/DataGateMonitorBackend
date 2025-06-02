using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;

public interface IOpenVpnMicroserviceClientFactory
{
    OpenVpnMicroserviceClient Create(OpenVpnServer server);
    Task<OpenVpnMicroserviceClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken);
}
