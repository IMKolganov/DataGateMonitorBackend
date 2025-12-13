namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

public class LoginResponse
{
    public string Token { get; set; } = default!;
    public DateTimeOffset Expiration { get; set; }

    public int UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}