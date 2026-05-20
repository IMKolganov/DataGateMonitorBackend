using System.Security.Cryptography;
using System.Text;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.Login;

public sealed class UserSessionService(
    IUserRefreshTokenQueryService refreshTokenQueryService,
    ICommandService<UserRefreshToken, int> refreshTokenCommandService,
    IConfiguration configuration) : IUserSessionService
{
    public async Task<GetUserSessionsResponse> GetActiveSessionsAsync(
        int userId,
        string? currentRefreshToken,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        string? currentHash = null;
        if (!string.IsNullOrWhiteSpace(currentRefreshToken))
            currentHash = HashRefreshToken(currentRefreshToken);

        var rows = await refreshTokenQueryService.Search(
            t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now,
            ct);

        var sessions = rows
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new UserSessionDto
            {
                Id = t.Id,
                DeviceId = t.DeviceId,
                UserAgent = t.UserAgent,
                CreatedAt = t.CreatedAt,
                ExpiresAt = t.ExpiresAt,
                IsCurrent = currentHash != null && t.TokenHash == currentHash,
            })
            .ToList();

        return new GetUserSessionsResponse { Sessions = sessions };
    }

    public async Task RevokeSessionAsync(int userId, int sessionId, CancellationToken ct)
    {
        var token = await refreshTokenQueryService.GetById(sessionId, ct)
                    ?? throw new InvalidOperationException("Session not found.");

        if (token.UserId != userId)
            throw new UnauthorizedAccessException("Session not found.");

        if (token.RevokedAt != null)
            return;

        token.RevokedAt = DateTimeOffset.UtcNow;
        await refreshTokenCommandService.Update(token, saveChanges: true, ct);
    }

    public async Task<int> RevokeSessionsAsync(int userId, string? keepRefreshToken, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        string? keepHash = null;
        if (!string.IsNullOrWhiteSpace(keepRefreshToken))
            keepHash = HashRefreshToken(keepRefreshToken);

        var rows = await refreshTokenQueryService.Search(
            t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now,
            ct);

        var revoked = 0;
        foreach (var token in rows)
        {
            if (keepHash != null && token.TokenHash == keepHash)
                continue;

            token.RevokedAt = now;
            await refreshTokenCommandService.Update(token, saveChanges: true, ct);
            revoked++;
        }

        return revoked;
    }

    public async Task RevokeByRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = HashRefreshToken(refreshToken);
        var token = await refreshTokenQueryService.GetByTokenHash(hash, ct);
        if (token is null || token.RevokedAt != null)
            return;

        token.RevokedAt = DateTimeOffset.UtcNow;
        await refreshTokenCommandService.Update(token, saveChanges: true, ct);
    }

    private string HashRefreshToken(string refreshToken)
    {
        var pepper = configuration["Jwt:RefreshPepper"]
                     ?? throw new InvalidOperationException("Jwt:RefreshPepper is not configured.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
