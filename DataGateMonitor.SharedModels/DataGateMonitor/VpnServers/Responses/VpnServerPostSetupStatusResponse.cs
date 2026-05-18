namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerPostSetupStatusResponse
{
    public string OperationId { get; set; } = string.Empty;
    public int VpnServerId { get; set; }
    public VpnServerPostSetupState State { get; set; } = VpnServerPostSetupState.Queued;
    public string Message { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public Dictionary<string, string> Details { get; set; } = [];
}
