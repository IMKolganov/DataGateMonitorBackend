namespace DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Responses;

public sealed class NodeKeyStatusResponse
{
    public string? TrustedFingerprint { get; set; }
    public string? PendingFingerprint { get; set; }
}
