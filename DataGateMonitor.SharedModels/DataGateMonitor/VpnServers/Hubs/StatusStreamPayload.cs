using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Hubs;

/// <summary>
/// SignalR payload broadcast on <c>OpenVpnStatusHub</c> (<c>StatusUpdated</c> event).
/// </summary>
public sealed class StatusStreamPayload
{
    public List<ServiceStatusResponse> Statuses { get; set; } = new();

    /// <summary>
    /// Distinct non-empty <c>ExternalId</c> among currently connected clients across all
    /// servers in <see cref="Statuses"/> (global unique devices; not a sum of per-server counts).
    /// </summary>
    public int TotalDevices { get; set; }

    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
}
