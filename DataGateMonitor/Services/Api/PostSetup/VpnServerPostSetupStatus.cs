namespace DataGateMonitor.Services.Api.PostSetup;

public class VpnServerPostSetupStatus
{
    public string OperationId { get; set; } = string.Empty;
    public int VpnServerId { get; set; }
    public VpnServerPostSetupState State { get; set; } = VpnServerPostSetupState.Queued;
    public string Message { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public bool IsCompleted => State is VpnServerPostSetupState.Succeeded or VpnServerPostSetupState.Failed;
    public Dictionary<string, string> Details { get; set; } = [];
}
