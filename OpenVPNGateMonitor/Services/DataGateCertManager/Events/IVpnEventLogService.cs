
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.VpnEvent.Requests;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public interface IVpnEventLogService
{
    Task SaveEventAsync(int vpnServerId, string eventType, VpnEventRequest e, CancellationToken ct);
}
