
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.VpnEvent.Requests;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Events;

public interface IVpnEventLogService
{
    Task SaveEventAsync(int vpnServerId, string eventType, VpnEventRequest e, CancellationToken ct);
}