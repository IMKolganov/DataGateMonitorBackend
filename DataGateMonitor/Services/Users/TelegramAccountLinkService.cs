using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users;

public sealed class TelegramAccountLinkService(
    IMemoryCache cache,
    IUserQueryService userQueryService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IUserMergeService userMergeService,
    IConfiguration configuration,
    ILogger<TelegramAccountLinkService> logger) : ITelegramAccountLinkService
{
    private const int DefaultCodeExpirationMinutes = 15;
    private const int CodeLength = 8;
    private const string TelegramProvider = AuthIdentityProviders.Telegram;
    private const string GoogleProvider = AuthIdentityProviders.Google;
    private const string LocalProvider = AuthIdentityProviders.Local;

    private sealed record AccountLinkCacheEntry(int DashboardUserId, long ExpectedTelegramId);

    public async Task<RequestTelegramAccountLinkCodeResponse> RequestLinkCodeAsync(
        int userId,
        long telegramId,
        CancellationToken ct)
    {
        if (userId <= 0)
            throw new ArgumentException("User id is required.", nameof(userId));

        if (telegramId <= 0)
            throw new ArgumentException("Telegram id is required.", nameof(telegramId));

        var user = await userQueryService.GetById(userId, ct)
                   ?? throw new KeyNotFoundException("User not found.");

        if (user.IsBlocked)
            throw new InvalidOperationException("User account is blocked.");

        var links = await userIdentityLinkQueryService.GetListByUserId(userId, ct);

        if (links.Any(l => string.Equals(l.Provider, TelegramProvider, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("This account is already linked to Telegram.");

        var hasGoogle = links.Any(l => string.Equals(l.Provider, GoogleProvider, StringComparison.OrdinalIgnoreCase));
        var hasLocal = links.Any(l => string.Equals(l.Provider, LocalProvider, StringComparison.OrdinalIgnoreCase));

        if (!hasGoogle && !hasLocal)
            throw new InvalidOperationException(
                "This account has no Google or password identity to link. Sign in with Google or register with a password first.");

        var existingTelegramLink = await userIdentityLinkQueryService.GetByProviderAndExternalId(
            TelegramProvider,
            telegramId.ToString(),
            ct);

        if (existingTelegramLink is not { UserId: > 0 })
            throw new InvalidOperationException(
                "Telegram account is not registered. Use /register in the bot first.");

        InvalidateActiveCodeForUser(userId);

        var code = GenerateCode();
        var minutes = configuration.GetValue<int?>("Auth:TelegramAccountLinkCodeMinutes") ?? DefaultCodeExpirationMinutes;
        if (minutes <= 0)
            minutes = DefaultCodeExpirationMinutes;

        var expiry = TimeSpan.FromMinutes(minutes);
        var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry };

        cache.Set(
            AccountLinkCacheKey(code),
            new AccountLinkCacheEntry(userId, telegramId),
            cacheOptions);
        cache.Set(UserActiveCodeKey(userId), code, cacheOptions);

        logger.LogInformation(
            "Account link code issued for user {UserId}, TelegramId={TelegramId}, valid {Minutes} minutes",
            userId,
            telegramId,
            minutes);

        return new RequestTelegramAccountLinkCodeResponse
        {
            Code = code,
            ExpiresInSeconds = (int)expiry.TotalSeconds,
        };
    }

    public async Task<CompleteTelegramAccountLinkResponse> CompleteLinkByCodeAsync(
        string code,
        long telegramId,
        CancellationToken ct)
    {
        if (telegramId <= 0)
            return Fail("Telegram id is required.");

        var normalizedCode = code?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            return Fail("Link code is required.");

        if (!cache.TryGetValue(AccountLinkCacheKey(normalizedCode), out AccountLinkCacheEntry entry))
            return Fail("Invalid or expired link code.");

        if (entry.ExpectedTelegramId != telegramId)
            return Fail("This link code was issued for a different Telegram account.");

        var dashboardUserId = entry.DashboardUserId;

        var telegramLink = await userIdentityLinkQueryService.GetByProviderAndExternalId(
            TelegramProvider,
            telegramId.ToString(),
            ct);

        if (telegramLink is not { UserId: > 0 })
            return Fail("Telegram account is not registered. Use /register in the bot first.");

        var telegramUserId = telegramLink.UserId;

        if (telegramUserId == dashboardUserId)
        {
            RemoveCode(normalizedCode, dashboardUserId);
            return new CompleteTelegramAccountLinkResponse
            {
                Success = true,
                Message = "Accounts are already linked.",
            };
        }

        var dashboardLinks = await userIdentityLinkQueryService.GetListByUserId(dashboardUserId, ct);
        var dashboardTelegramLink = dashboardLinks.FirstOrDefault(l =>
            string.Equals(l.Provider, TelegramProvider, StringComparison.OrdinalIgnoreCase));

        if (dashboardTelegramLink is not null
            && !string.Equals(dashboardTelegramLink.ExternalId, telegramId.ToString(), StringComparison.Ordinal))
        {
            return Fail("This dashboard account is already linked to a different Telegram account.");
        }

        var hasGoogle = dashboardLinks.Any(l =>
            string.Equals(l.Provider, GoogleProvider, StringComparison.OrdinalIgnoreCase));
        var hasLocal = dashboardLinks.Any(l =>
            string.Equals(l.Provider, LocalProvider, StringComparison.OrdinalIgnoreCase));

        if (!hasGoogle && !hasLocal)
            return Fail("Dashboard account has no Google or password identity to merge.");

        try
        {
            var merge = await userMergeService.MergeTelegramGoogleAsync(
                new MergeTelegramGoogleUsersRequest
                {
                    TelegramUserId = telegramUserId,
                    GoogleUserId = dashboardUserId,
                    Note = $"Telegram bot account link (telegramId={telegramId})",
                },
                performedByUserId: telegramUserId,
                ct);

            RemoveCode(normalizedCode, dashboardUserId);

            return new CompleteTelegramAccountLinkResponse
            {
                Success = true,
                Message = "Accounts linked successfully.",
                Merge = merge,
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Account link merge failed for TelegramId={TelegramId}, DashboardUserId={DashboardUserId}",
                telegramId,
                dashboardUserId);

            return Fail("Account link failed. Please try again or request a new code.");
        }
    }

    private void InvalidateActiveCodeForUser(int userId)
    {
        if (!cache.TryGetValue(UserActiveCodeKey(userId), out string? previousCode)
            || string.IsNullOrWhiteSpace(previousCode))
        {
            return;
        }

        cache.Remove(AccountLinkCacheKey(previousCode));
        cache.Remove(UserActiveCodeKey(userId));
    }

    private void RemoveCode(string normalizedCode, int dashboardUserId)
    {
        cache.Remove(AccountLinkCacheKey(normalizedCode));
        cache.Remove(UserActiveCodeKey(dashboardUserId));
    }

    private static CompleteTelegramAccountLinkResponse Fail(string message)
        => new() { Success = false, Message = message };

    private static string AccountLinkCacheKey(string code) => $"account-link:code:{code}";

    private static string UserActiveCodeKey(int userId) => $"account-link:user:{userId}";

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
}
