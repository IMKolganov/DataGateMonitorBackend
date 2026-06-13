namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public class AuthTokensResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset Expiration { get; set; }

    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshExpiration { get; set; }
}