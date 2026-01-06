using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth.Login;

public sealed class TokenService(
    IConfiguration configuration,
    IUserQueryService userQueryService,
    IUserRoleService userRoleService,
    ICommandService<UserRefreshToken, int> refreshTokenCommandService
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

        var (accessToken, accessExpiresAt) = await CreateAccessTokenAsync(user, externalId, ct);

        var refreshLifetimeDays = configuration.GetValue<int?>("Jwt:RefreshLifetimeDays") ?? 30;
        if (refreshLifetimeDays <= 0)
            refreshLifetimeDays = 30;

        var now = DateTimeOffset.UtcNow;
        var refreshExpiresAt = now.AddDays(refreshLifetimeDays);

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

        return new TokenPair(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt);
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
