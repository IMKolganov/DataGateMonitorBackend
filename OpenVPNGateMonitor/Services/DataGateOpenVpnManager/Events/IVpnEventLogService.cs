
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.VpnEvent.Requests;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Events;

public interface IVpnEventLogService
{
    Task SaveEventAsync(int vpnServerId, string eventType, VpnEventRequest e, CancellationToken ct);
}