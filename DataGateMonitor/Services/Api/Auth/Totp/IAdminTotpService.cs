using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.Totp;

public interface IAdminTotpService
{
    Task<bool> IsAdminUserAsync(int userId, CancellationToken ct);

    bool IsTotpEnabled(UserCredential? credential);

    Task<LoginResponse> ApplyAdminTotpGateAsync(
        User user,
        UserCredential? credential,
        string? externalId,
        string? deviceId,
        string? userAgent,
        Func<CancellationToken, Task<LoginResponse>> issueTokensAsync,
        CancellationToken ct);

    Task<LoginResponse> VerifyLoginChallengeAsync(TotpVerifyLoginRequest request, CancellationToken ct);

    Task<TotpStatusResponse> GetStatusAsync(int userId, CancellationToken ct);

    Task<TotpSetupResponse> BeginSetupAsync(int userId, CancellationToken ct);

    Task ConfirmSetupAsync(int userId, TotpConfirmRequest request, CancellationToken ct);

    Task DisableAsync(int userId, TotpDisableRequest request, CancellationToken ct);
}
