namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

public sealed class ConfirmEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
