namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

public sealed class KillOpenVpnClientResponse
{
    public bool Success { get; set; }
    public bool RevokeAttempted { get; set; }
    public bool? RevokeSucceeded { get; set; }
    public string? ErrorMessage { get; set; }
}
