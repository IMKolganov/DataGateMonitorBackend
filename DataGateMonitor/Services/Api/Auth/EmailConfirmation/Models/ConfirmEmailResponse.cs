namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation.Models;

public sealed class ConfirmEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
