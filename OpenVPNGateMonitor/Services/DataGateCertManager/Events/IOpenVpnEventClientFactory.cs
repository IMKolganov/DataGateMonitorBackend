using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public interface IOpenVpnEventClientFactory
{
    OpenVpnEventClient Create(OpenVpnServer server);
    Task<OpenVpnEventClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken);
    bool Remove(int serverId);
    IReadOnlyCollection<OpenVpnEventClient> GetAllClients();
    IReadOnlyCollection<OpenVpnEventConnectionStatus> GetAllClientStatuses();
    bool TryGetClientStatus(int serverId, out OpenVpnEventConnectionStatus? status);
}
