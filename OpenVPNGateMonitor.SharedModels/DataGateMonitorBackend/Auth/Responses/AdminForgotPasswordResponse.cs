namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

/// <summary>
/// Same message for all cases to avoid revealing whether the account exists or is admin.
/// If the account is an admin with password sign-in, a code was written to the server console.
/// </summary>
public sealed class AdminForgotPasswordResponse
{
    public string Message { get; set; } = null!;
}
