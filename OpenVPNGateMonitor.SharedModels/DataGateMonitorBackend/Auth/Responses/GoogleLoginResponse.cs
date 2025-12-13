namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

public sealed class GoogleLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset Expiration { get; set; }

    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsNewUser { get; set; }
}