using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.AdminEmail;
using DataGateMonitor.Services.EmailTemplates;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation.Models;

namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public sealed class EmailConfirmationService(
    IMemoryCache cache,
    IUserQueryService userQueryService,
    ICommandService<User, int> userCommandService,
    IEmailSenderService emailSenderService,
    ISettingsService settingsService,
    ISentEmailLogService sentEmailLogService,
    ISystemTransactionalEmailService systemTransactionalEmail) : IEmailConfirmationService
{
    private const int CodeLength = 6;
    private const int DefaultCodeTtlMinutes = 30;
    private const int RateLimitWindowMinutes = 10;
    private const int RateLimitMaxRequests = 3;
    private static readonly ConcurrentDictionary<string, object> RateLimitLocks = new();

    public async Task SendConfirmationAsync(int userId, string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var rateLimitKey = $"email-confirm:rate:{normalizedEmail}";
        if (IsRateLimited(rateLimitKey))
            throw new InvalidOperationException("Too many confirmation requests. Try again later.");

        RecordRateLimit(rateLimitKey);

        var codeTtlMinutes = await GetCodeTtlMinutesAsync(ct);

        var code = GenerateCode();
        cache.Set(
            key: GetCodeCacheKey(normalizedEmail),
            value: new EmailConfirmationCodePayload(userId, code),
            options: new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(codeTtlMinutes)
            });

        var (subject, body) = await systemTransactionalEmail.GetEmailConfirmationAsync(code, codeTtlMinutes, ct);
        try
        {
            await emailSenderService.SendAsync(email, subject, body, ct);
            await sentEmailLogService.LogAsync(userId, email, subject, body, true, null, null, ct);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (msg.Length > 4000)
                msg = msg[..4000];
            try
            {
                await sentEmailLogService.LogAsync(userId, email, subject, body, false, msg, null, ct);
            }
            catch
            {
                // ignore secondary logging failures
            }

            throw;
        }
    }

    public async Task<ConfirmEmailResponse> ConfirmAsync(string email, string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            return new ConfirmEmailResponse { Success = false, Message = "Email and code are required." };

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var cacheKey = GetCodeCacheKey(normalizedEmail);

        if (!cache.TryGetValue(cacheKey, out EmailConfirmationCodePayload? payload) || payload is null)
            return new ConfirmEmailResponse { Success = false, Message = "Invalid or expired confirmation code." };

        if (!string.Equals(payload.Code, code.Trim(), StringComparison.Ordinal))
            return new ConfirmEmailResponse { Success = false, Message = "Invalid or expired confirmation code." };

        var user = await userQueryService.GetById(payload.UserId, ct);
        if (user is null)
            return new ConfirmEmailResponse { Success = false, Message = "User not found." };

        if (!string.Equals(user.Email?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
            return new ConfirmEmailResponse { Success = false, Message = "Email mismatch." };

        user.IsEmailConfirmed = true;
        await userCommandService.Update(user, saveChanges: true, ct);
        cache.Remove(cacheKey);

        return new ConfirmEmailResponse { Success = true, Message = "Email confirmed successfully." };
    }

    private static string GetCodeCacheKey(string normalizedEmail) => $"email-confirm:code:{normalizedEmail}";

    private static string GenerateCode()
    {
        const string chars = "0123456789";
        var bytes = new byte[CodeLength];
        RandomNumberGenerator.Fill(bytes);
        var result = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
            result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }

    private bool IsRateLimited(string cacheKey)
    {
        var lockObj = RateLimitLocks.GetOrAdd(cacheKey, _ => new object());
        lock (lockObj)
        {
            if (!cache.TryGetValue(cacheKey, out RateLimitEntry? entry) || entry is null)
                return false;

            var now = DateTimeOffset.UtcNow;
            if (now - entry.FirstRequestUtc > TimeSpan.FromMinutes(RateLimitWindowMinutes))
                return false;

            return entry.Count >= RateLimitMaxRequests;
        }
    }

    private void RecordRateLimit(string cacheKey)
    {
        var lockObj = RateLimitLocks.GetOrAdd(cacheKey, _ => new object());
        lock (lockObj)
        {
            var now = DateTimeOffset.UtcNow;
            if (!cache.TryGetValue(cacheKey, out RateLimitEntry? entry) || entry is null)
            {
                cache.Set(cacheKey, new RateLimitEntry(now, 1), TimeSpan.FromMinutes(RateLimitWindowMinutes + 1));
                return;
            }

            if (now - entry.FirstRequestUtc > TimeSpan.FromMinutes(RateLimitWindowMinutes))
            {
                cache.Set(cacheKey, new RateLimitEntry(now, 1), TimeSpan.FromMinutes(RateLimitWindowMinutes + 1));
                return;
            }

            cache.Set(cacheKey, new RateLimitEntry(entry.FirstRequestUtc, entry.Count + 1),
                TimeSpan.FromMinutes(RateLimitWindowMinutes + 1));
        }
    }

    private sealed record EmailConfirmationCodePayload(int UserId, string Code);
    private sealed record RateLimitEntry(DateTimeOffset FirstRequestUtc, int Count);

    private async Task<int> GetCodeTtlMinutesAsync(CancellationToken ct)
    {
        var typeKey = $"{AuthEmailSettingsKeys.ConfirmationCodeTtlMinutes}_Type";
        var type = await settingsService.GetValueAsync<string>(typeKey, ct);
        if (!string.Equals(type, "int", StringComparison.OrdinalIgnoreCase))
            return DefaultCodeTtlMinutes;

        var value = await settingsService.GetValueAsync<int>(AuthEmailSettingsKeys.ConfirmationCodeTtlMinutes, ct);
        if (value is < 1 or > 1440)
            return DefaultCodeTtlMinutes;

        return value;
    }

}
