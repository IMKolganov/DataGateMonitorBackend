using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public interface IVpnEventLogService
{
    Task SaveEventAsync(int vpnServerId, string eventType, OpenVpnServerEventLog data, string rawJson, CancellationToken cancellationToken);
}
