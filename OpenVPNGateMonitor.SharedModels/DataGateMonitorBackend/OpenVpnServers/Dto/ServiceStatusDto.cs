using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

public class ServiceStatusDto
{
    public int VpnServerId { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
    public int CountConnectedClients { get; set; }
    public int CountSessions { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset NextRunTime { get; set; }
}