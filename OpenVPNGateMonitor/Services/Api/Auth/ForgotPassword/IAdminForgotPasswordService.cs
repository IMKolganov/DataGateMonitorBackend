using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.ForgotPassword;

public interface IAdminForgotPasswordService
{
    /// <summary>
    /// Requests a one-time reset code for an admin that uses password sign-in.
    /// If eligible, the code is written to the server console only (no email).
    /// Does not reveal whether the account exists or is admin; same response for all cases.
    /// </summary>
    /// <param name="request">Login or email.</param>
    /// <param name="clientIp">Client IP for rate limiting.</param>
    /// <param name="ct">Cancellation.</param>
    Task<AdminForgotPasswordResponse> RequestResetCodeAsync(
        AdminForgotPasswordRequest request,
        string? clientIp,
        CancellationToken ct);

    /// <summary>
    /// Resets password using the code from server console.
    /// </summary>
    Task<AdminResetPasswordResponse> ResetPasswordAsync(
        AdminResetPasswordRequest request,
        CancellationToken ct);
}
