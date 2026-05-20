using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.Login;

public interface IUserSessionService
{
    Task<GetUserSessionsResponse> GetActiveSessionsAsync(int userId, string? currentRefreshToken, CancellationToken ct);

    Task RevokeSessionAsync(int userId, int sessionId, CancellationToken ct);

    /// <summary>Revokes all sessions; optionally keeps the one matching <paramref name="keepRefreshToken"/>.</summary>
    Task<int> RevokeSessionsAsync(int userId, string? keepRefreshToken, CancellationToken ct);

    Task RevokeByRefreshTokenAsync(string refreshToken, CancellationToken ct);
}
