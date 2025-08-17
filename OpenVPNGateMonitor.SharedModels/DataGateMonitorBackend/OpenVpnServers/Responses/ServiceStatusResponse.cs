using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

public class ServiceStatusResponse
{
    public int VpnServerId { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset NextRunTime { get; set; }
}