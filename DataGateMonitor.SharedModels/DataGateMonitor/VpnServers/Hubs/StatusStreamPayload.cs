using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Hubs;

/// <summary>
/// SignalR payload broadcast on <c>OpenVpnStatusHub</c> (<c>StatusUpdated</c> event).
/// </summary>
public sealed class StatusStreamPayload
{
    public List<ServiceStatusResponse> Statuses { get; set; } = new();

    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
}
