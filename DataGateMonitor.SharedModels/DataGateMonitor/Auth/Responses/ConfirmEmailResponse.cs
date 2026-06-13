namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public sealed class ConfirmEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
