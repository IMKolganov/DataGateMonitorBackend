namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>
/// Request for admin forgot-password. Accepts login or email.
/// Only password-based admin accounts are eligible; response does not reveal existence or count of admins.
/// </summary>
public sealed class AdminForgotPasswordRequest
{
    /// <summary>Admin login or email.</summary>
    public string LoginOrEmail { get; set; } = null!;
}
