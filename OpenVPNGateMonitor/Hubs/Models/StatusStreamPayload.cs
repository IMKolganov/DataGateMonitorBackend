using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Hubs.Models;

public sealed class StatusStreamPayload
{
    public List<ServiceStatusResponse> Statuses { get; init; } = new();
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}