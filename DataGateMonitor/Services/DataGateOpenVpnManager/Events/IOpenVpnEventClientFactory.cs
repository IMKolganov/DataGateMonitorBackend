using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Events;

public interface IOpenVpnEventClientFactory
{
    OpenVpnEventClient Create(VpnServer server);
    Task<OpenVpnEventClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken);
    bool Remove(int serverId);
    IReadOnlyCollection<OpenVpnEventClient> GetAllClients();
    ConnectionStatusesResponse GetAllClientStatuses();
    bool TryGetClientStatus(int serverId, out ConnectionStatusResponse? status);
}