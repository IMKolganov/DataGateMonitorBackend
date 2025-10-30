using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public interface IOpenVpnEventClientFactory
{
    OpenVpnEventClient Create(OpenVpnServer server);
    Task<OpenVpnEventClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken);
    bool Remove(int serverId);
    IReadOnlyCollection<OpenVpnEventClient> GetAllClients();
    ConnectionStatusesResponse GetAllClientStatuses();
    bool TryGetClientStatus(int serverId, out ConnectionStatusResponse? status);
}