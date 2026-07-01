using DataGateMonitor.SharedModels.DataGateOpenVpnManager.PiHole.Requests;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Events;

public interface IVpnDnsQueryLogService
{
    Task<int> SaveBatchAsync(int vpnServerId, DnsQueryBatchRequest batch, CancellationToken ct);
}
