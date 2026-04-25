namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation.Models;

public sealed class RequestEmailConfirmationRequest
{
    public string Email { get; set; } = string.Empty;
}
