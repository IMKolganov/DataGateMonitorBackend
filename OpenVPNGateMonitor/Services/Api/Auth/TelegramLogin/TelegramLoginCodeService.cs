using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.Services.Api.Auth.Login;
using OpenVPNGateMonitor.Services.Users.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.TelegramLogin;

public sealed class TelegramLoginCodeService(
    ITelegramBotUserQueryService telegramBotUserQueryService,
    IUserService userService,
    ITokenService tokenService,
    IMemoryCache cache,
    IHttpContextAccessor httpContextAccessor) : ITelegramLoginCodeService
{
    private const int CodeExpirationMinutes = 10;
    private const int CodeLength = 8;

    public async Task<TelegramRequestLoginCodeResponse?> RequestLoginCodeAsync(
        TelegramRequestLoginCodeRequest request,
        CancellationToken ct)
    {
        var telegramBotUser = await telegramBotUserQueryService.GetByTelegramId(request.TelegramId, ct);
        if (telegramBotUser == null)
            return null;

        if (telegramBotUser.IsBlocked)
            return null;

        var code = GenerateCode();
        var expiry = TimeSpan.FromMinutes(CodeExpirationMinutes);
        cache.Set(TelegramCodeCacheKey(code), request.TelegramId, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        });

        return new TelegramRequestLoginCodeResponse
        {
            Code = code,
            ExpiresInSeconds = (int)expiry.TotalSeconds
        };
    }

    public async Task<LoginResponse> LoginWithCodeAsync(TelegramCodeLoginRequest request, CancellationToken ct)
    {
        var code = request.Code?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new UnauthorizedAccessException("Invalid or expired code.");

        var cacheKey = TelegramCodeCacheKey(code);
        if (!cache.TryGetValue(cacheKey, out long telegramId))
            throw new UnauthorizedAccessException("Invalid or expired code.");

        cache.Remove(cacheKey);

        var user = await userService.GetOrCreateDashboardUserForTelegramAsync(telegramId, ct);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid or expired code.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("User account is blocked.");

        var (deviceId, userAgent) = GetClientInfo();

        var tokenPair = await tokenService.IssueAsync(
            userId: user.Id,
            externalId: telegramId.ToString(),
            deviceId: deviceId,
            userAgent: userAgent,
            ct: ct);

        return new LoginResponse
        {
            Token = tokenPair.AccessToken,
            Expiration = tokenPair.AccessExpiresAt,
            RefreshToken = tokenPair.RefreshToken,
            RefreshExpiration = tokenPair.RefreshExpiresAt,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
        };
    }

    private static string TelegramCodeCacheKey(string code) => $"telegram_login:{code.ToUpperInvariant()}";

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[CodeLength];
        RandomNumberGenerator.Fill(bytes);
        var result = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
            result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }

    private (string? deviceId, string? userAgent) GetClientInfo()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null)
            return (null, null);
        var userAgent = ctx.Request.Headers.UserAgent.ToString();
        var deviceId = ctx.Request.Headers["X-Device-Id"].ToString();
        if (string.IsNullOrWhiteSpace(deviceId))
            deviceId = null;
        return (deviceId, userAgent);
    }
}
