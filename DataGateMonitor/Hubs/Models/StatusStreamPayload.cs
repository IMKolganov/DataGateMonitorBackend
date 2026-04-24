using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.Hubs.Models;

public sealed class StatusStreamPayload
{
    public List<ServiceStatusResponse> Statuses { get; init; } = new();
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}