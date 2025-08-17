using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Models.Helpers.Background;

public class BackgroundServerStatus
{
    public int VpnServerId { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset NextRunTime { get; set; }
    public int CountConnectedClients { get; set; }
    public int CountSessions { get; set; }
    public int TotalBytesIn { get; set; }
    public int TotalBytesOut { get; set; }
}