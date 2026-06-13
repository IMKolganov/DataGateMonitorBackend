namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;


public sealed class RegisterUserResponse
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool HasDashboardAccess { get; set; }
}