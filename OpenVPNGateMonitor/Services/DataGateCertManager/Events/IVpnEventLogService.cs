
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.VpnEvent.Requests;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public interface IVpnEventLogService
{
    Task SaveEventAsync(int vpnServerId, string eventType, VpnEventRequest e, CancellationToken ct);
    // Task SaveEventAsync(int vpnServerId, string eventType, OpenVpnServerEventLog data, string rawJson, 
    //     CancellationToken cancellationToken);
    //
    // Task<VpnServerEventResponse> GetEventByVpnServerIdAsync(int vpnServerId, int page, int pageSize,
    //     CancellationToken cancellationToken);
}
