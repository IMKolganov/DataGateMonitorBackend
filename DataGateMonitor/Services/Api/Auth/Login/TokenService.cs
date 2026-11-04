using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Api.Auth.Users;

namespace DataGateMonitor.Services.Api.Auth.Login;

public sealed class TokenService(
    IConfiguration configuration,
    IUserQueryService userQueryService,
    IUserRoleService userRoleService,
    IUserRefreshTokenQueryService refreshTokenQueryService,
    ICommandService<UserRefreshToken, int> refreshTokenCommandService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IAdminIdleSessionTracker adminIdleSessionTracker,
    IAdminIdleTimeoutProvider adminIdleTimeoutProvider
) : ITokenService
{
    public async Task<TokenPair> IssueAsync(
        int userId,
        string? externalId,
        string? deviceId,
        string? userAgent,
        CancellationToken ct)
    {
        var user = await userQueryService.GetById(userId, ct)
                   ?? throw new InvalidOperationException("User not found.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("User account is blocked.");

        var resolvedExternalId = await ResolveExternalIdAsync(user, externalId, ct);
        var (accessToken, accessExpiresAt) = await CreateAccessTokenAsync(user, resolvedExternalId, ct);

        var refreshLifetimeDays = configuration.GetValue<int?>("Jwt:RefreshLifetimeDays") ?? 30;
        if (refreshLifetimeDays <= 0)
            refreshLifetimeDays = 30;

        var now = DateTimeOffset.UtcNow;
        var refreshExpiresAt = now.AddDays(refreshLifetimeDays);

        // Re-login on the same device replaces the previous refresh token instead of stacking.
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            await refreshTokenCommandService.UpdateWhere(
                t => t.UserId == user.Id
                     && t.DeviceId == deviceId
                     && t.RevokedAt == null,
                u => u.SetProperty(x => x.RevokedAt, now),
                ct);
        }

        var refreshToken = GenerateRefreshToken();
        var refreshHash = HashRefreshToken(refreshToken);

        var userRefreshToken = new UserRefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = now,
            ExpiresAt = refreshExpiresAt,
            RevokedAt = null,
            ReplacedByTokenId = null,
            DeviceId = deviceId,
            UserAgent = userAgent
        };

        await refreshTokenCommandService.Add(userRefreshToken, saveChanges: true, ct);

        var roleOnIssue = await userRoleService.GetUserRoleNameAsync(user.Id, ct);
        if (AdminIdleSessionTracker.IsAdminRole(roleOnIssue))
            adminIdleSessionTracker.Touch(user.Id);

        return new TokenPair(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt);
    }

    public async Task<TokenPair> RefreshAsync(
        string refreshToken,
        string? deviceId,
        string? userAgent,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("RefreshToken is required.", nameof(refreshToken));

        var now = DateTimeOffset.UtcNow;

        var tokenHash = HashRefreshToken(refreshToken);
        var existing = await refreshTokenQueryService.GetByTokenHash(tokenHash, ct);

        if (existing is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (existing.ExpiresAt <= now)
            throw new UnauthorizedAccessException("Refresh token is expired.");

        if (!string.IsNullOrWhiteSpace(deviceId) && existing.DeviceId != null && existing.DeviceId != deviceId)
            throw new UnauthorizedAccessException("Invalid device.");

        // Reuse of an already-rotated token. Concurrent tabs often retry the old token right after
        // another request won the rotation — that must NOT wipe the winner's session. Only treat as
        // theft (family revoke) when reuse happens after the grace window.
        if (existing.RevokedAt != null)
        {
            if (existing.ReplacedByTokenId != null && !IsWithinRefreshReuseGrace(existing.RevokedAt.Value, now))
                await RevokeAllActiveRefreshTokensForUserAsync(existing.UserId, now, ct);

            throw new UnauthorizedAccessException("Refresh token is revoked.");
        }

        // Fail closed before rotating so a blocked/idle user does not orphan a new refresh row.
        var user = await userQueryService.GetById(existing.UserId, ct)
                   ?? throw new InvalidOperationException("User not found.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("User account is blocked.");

        var roleOnRefresh = await userRoleService.GetUserRoleNameAsync(user.Id, ct);
        if (AdminIdleSessionTracker.IsAdminRole(roleOnRefresh))
        {
            if (adminIdleSessionTracker.IsExpired(user.Id))
                throw new UnauthorizedAccessException("Administrator session expired due to inactivity.");
        }

        var refreshLifetimeDays = configuration.GetValue<int?>("Jwt:RefreshLifetimeDays") ?? 30;
        if (refreshLifetimeDays <= 0)
            refreshLifetimeDays = 30;

        var newRefreshToken = GenerateRefreshToken();
        var newRefreshHash = HashRefreshToken(newRefreshToken);
        var newRefreshExpiresAt = now.AddDays(refreshLifetimeDays);

        var newEntity = new UserRefreshToken
        {
            UserId = existing.UserId,
            TokenHash = newRefreshHash,
            CreatedAt = now,
            ExpiresAt = newRefreshExpiresAt,
            RevokedAt = null,
            ReplacedByTokenId = null,
            DeviceId = existing.DeviceId ?? deviceId,
            UserAgent = existing.UserAgent ?? userAgent
        };

        await refreshTokenCommandService.Add(newEntity, saveChanges: true, ct);

        // Atomic claim: only one concurrent refresh wins. Loser must not revoke the winner's family.
        var claimed = await refreshTokenCommandService.UpdateWhere(
            t => t.Id == existing.Id && t.RevokedAt == null,
            u => u.SetProperty(x => x.RevokedAt, now)
                .SetProperty(x => x.ReplacedByTokenId, (long?)newEntity.Id),
            ct);

        if (claimed == 0)
        {
            await refreshTokenCommandService.Delete(newEntity, saveChanges: true, ct);
            throw new UnauthorizedAccessException("Refresh token is revoked.");
        }

        if (AdminIdleSessionTracker.IsAdminRole(roleOnRefresh))
            adminIdleSessionTracker.Touch(user.Id);

        var resolvedExternalId = await ResolveExternalIdAsync(user, externalId: null, ct);

        var (accessToken, accessExpiresAt) = await CreateAccessTokenAsync(user, resolvedExternalId, ct);

        return new TokenPair(accessToken, accessExpiresAt, newRefreshToken, newRefreshExpiresAt);
    }

    private bool IsWithinRefreshReuseGrace(DateTimeOffset revokedAt, DateTimeOffset now)
    {
        var graceSeconds = configuration.GetValue<int?>("Jwt:RefreshReuseGraceSeconds") ?? 60;
        if (graceSeconds <= 0)
            return false;
        return revokedAt.AddSeconds(graceSeconds) >= now;
    }

    private Task<int> RevokeAllActiveRefreshTokensForUserAsync(
        int userId,
        DateTimeOffset now,
        CancellationToken ct) =>
        refreshTokenCommandService.UpdateWhere(
            t => t.UserId == userId && t.RevokedAt == null,
            u => u.SetProperty(x => x.RevokedAt, now),
            ct);
    
    private async Task<string?> ResolveExternalIdAsync(User user, string? externalId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(externalId))
            return externalId;

        return await UserIdentityLinkExternalIdResolver.ResolveAsync(
            user.Id,
            userIdentityLinkQueryService,
            ct);
    }

    private async Task<(string Token, DateTimeOffset ExpiresAt)> CreateAccessTokenAsync(User user, string? externalId, CancellationToken ct)
    {
        var secret = configuration["Jwt:Secret"]
                     ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var issuer = configuration["Jwt:Issuer"] ?? "OpenVPNGateBackend";
        var audience = configuration["Jwt:Audience"] ?? "OpenVPNGateFrontend";

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var lifetimeMinutes = configuration.GetValue<int?>("Jwt:LifetimeMinutes") ?? 15;
        if (lifetimeMinutes <= 0)
            lifetimeMinutes = 15;

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(lifetimeMinutes);

        var role = await userRoleService.GetUserRoleNameAsync(user.Id, ct);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName ?? string.Empty),
            new(ClaimTypes.Role, role),

            new("externalId", externalId ?? string.Empty),

            new("displayName", user.DisplayName ?? string.Empty),
            new("email", user.Email ?? string.Empty),
        };

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            claims.Add(new Claim("avatarUrl", user.AvatarUrl));

        if (AdminIdleSessionTracker.IsAdminRole(role))
        {
            var idleMinutes = await adminIdleTimeoutProvider.GetMinutesAsync(ct);
            claims.Add(new Claim("adminIdleTimeoutMinutes", idleMinutes.ToString()));
        }

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(tokenDescriptor);

        return (token, expires);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncode(bytes);
    }

    private string HashRefreshToken(string refreshToken)
    {
        var pepper = configuration["Jwt:RefreshPepper"]
                     ?? throw new InvalidOperationException("Jwt:RefreshPepper is not configured.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Base64UrlEncode(hashBytes);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
