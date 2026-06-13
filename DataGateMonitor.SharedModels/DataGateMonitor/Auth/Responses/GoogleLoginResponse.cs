namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public class GoogleLoginResponse : LoginResponse
{
    public bool IsNewUser { get; set; }
    public string? AvatarUrl { get; set; }
}
