namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

public class TokenResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset Expiration { get; set; }
}