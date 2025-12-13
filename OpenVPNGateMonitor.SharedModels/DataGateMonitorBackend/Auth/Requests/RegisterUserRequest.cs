namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;

public sealed class RegisterUserRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}