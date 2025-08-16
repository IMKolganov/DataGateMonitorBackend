
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.VpnEvent.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public interface IVpnEventLogService
{
    Task SaveEventAsync(int vpnServerId, string eventType, VpnEventRequest e, CancellationToken ct);
    Task<VpnServerEventResponse> GetEventByVpnServerIdAsync(int vpnServerId, int page, int pageSize,
        CancellationToken cancellationToken);
}
