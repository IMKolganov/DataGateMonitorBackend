namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

public sealed class AdminSetUserPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
