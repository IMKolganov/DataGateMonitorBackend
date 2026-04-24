namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public sealed class GoogleLoginResponse : AuthTokensResponse
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsNewUser { get; set; }
}