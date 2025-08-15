using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public interface IOpenVpnEventClientFactory
{
    OpenVpnEventClient Create(OpenVpnServer server);
    Task<OpenVpnEventClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken);
}
