namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public class LoginResponse : AuthTokensResponse
{
    public int UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}