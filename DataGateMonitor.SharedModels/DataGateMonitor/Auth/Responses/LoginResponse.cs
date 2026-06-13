namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public class LoginResponse
{
    public int UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Token { get; set; }
    public DateTimeOffset Expiration { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshExpiration { get; set; }

    /// <summary>Admin must complete TOTP verification before tokens are issued.</summary>
    public bool RequiresTotp { get; set; }

    /// <summary>Short-lived id from password/OAuth step; use with POST /api/auth/totp/verify-login.</summary>
    public string? LoginChallengeId { get; set; }

    /// <summary>Admin should enroll TOTP (may still receive tokens when false and RequiresTotp is false).</summary>
    public bool RequiresTotpSetup { get; set; }
}
