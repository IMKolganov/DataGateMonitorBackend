namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>
/// Request to set a new password using the code from server console.
/// </summary>
public sealed class AdminResetPasswordRequest
{
    /// <summary>One-time code shown in server console after forgot-password.</summary>
    public string Code { get; set; } = null!;

    /// <summary>New password.</summary>
    public string NewPassword { get; set; } = null!;

    /// <summary>Must match NewPassword.</summary>
    public string ConfirmPassword { get; set; } = null!;
}
