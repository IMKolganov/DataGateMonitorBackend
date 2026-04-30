using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserRoleTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.AdminEmail;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.Services.EmailTemplates;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.ForgotPassword;

public sealed class AdminForgotPasswordService(
    IUserCredentialQueryService credentialQueryService,
    IUserQueryService userQueryService,
    IUserRoleQueryService userRoleQueryService,
    ICommandService<UserCredential, int> credentialCommandService,
    IPasswordHasher<User> passwordHasher,
    IMemoryCache cache,
    IEmailSenderService emailSender,
    ISentEmailLogService sentEmailLogService,
    ISystemTransactionalEmailService systemTransactionalEmail,
    IAppNotificationFacade appNotificationFacade,
    ILogger<AdminForgotPasswordService> logger) : IAdminForgotPasswordService
{
    private const int RateLimitWindowMinutes = 15;
    private const int RateLimitMaxRequests = 5;
    private const int CodeExpirationMinutes = 15;
    private const int CodeLength = 10;

    private static readonly ConcurrentDictionary<string, object> RateLimitLocks = new();

    public const string SameMessageForAll =
        "If an admin account with this login exists and uses password sign-in, a reset code has been sent to the account email (when configured) and written to the server console. Otherwise, no such user was found.";

    public const string RateLimitMessage = "Too many requests. Try again later.";

    public async Task<AdminForgotPasswordResponse> RequestResetCodeAsync(
        AdminForgotPasswordRequest request,
        string? clientIp,
        CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(clientIp) ? "unknown" : clientIp;
        var cacheKey = $"forgotpwd:rl:{key}";

        if (IsRateLimited(cacheKey, out _))
            return new AdminForgotPasswordResponse { Message = RateLimitMessage };

        var loginOrEmail = request.LoginOrEmail?.Trim();
        if (string.IsNullOrWhiteSpace(loginOrEmail))
            return new AdminForgotPasswordResponse { Message = SameMessageForAll };

        RecordRateLimit(cacheKey);

        var normalizedLogin = loginOrEmail.ToUpperInvariant();
        var credential = await credentialQueryService.GetByNormalizedLogin(normalizedLogin, ct);

        if (credential is null)
        {
            var userByEmail = await userQueryService.GetByEmail(loginOrEmail, ct);
            if (userByEmail is null)
                return new AdminForgotPasswordResponse { Message = SameMessageForAll };

            credential = await credentialQueryService.GetByUserId(userByEmail.Id, ct);
            if (credential is null)
                return new AdminForgotPasswordResponse { Message = SameMessageForAll };
        }

        var user = await userQueryService.GetById(credential.UserId, ct);
        if (user is null)
            return new AdminForgotPasswordResponse { Message = SameMessageForAll };

        var role = await userRoleQueryService.GetByUserId(user.Id, ct);
        if (role is null || role.RoleId != SystemRoles.AdminId)
            return new AdminForgotPasswordResponse { Message = SameMessageForAll };

        var code = GenerateCode();
        var expiry = TimeSpan.FromMinutes(CodeExpirationMinutes);
        cache.Set($"forgotpwd:code:{code}", credential.UserId, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry });

        var (subject, body) = await systemTransactionalEmail.GetAdminPasswordResetAsync(code, CodeExpirationMinutes, ct);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var to = user.Email.Trim();
            try
            {
                await emailSender.SendAsync(to, subject, body, ct);
                await sentEmailLogService.LogAsync(user.Id, to, subject, body, true, null, null, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send admin reset email to {Email}", to);
                try
                {
                    var msg = ex.Message.Length > 4000 ? ex.Message[..4000] : ex.Message;
                    await sentEmailLogService.LogAsync(user.Id, to, subject, body, false, msg, null, ct);
                }
                catch
                {
                    // ignore secondary logging failures
                }
            }
        }

        logger.LogInformation(
            "Admin password reset code for login '{Login}' (userId={UserId}): {Code}. Valid for {Minutes} minutes. Check email (if sent) or server console.",
            credential.Login,
            credential.UserId,
            code,
            CodeExpirationMinutes);

        return new AdminForgotPasswordResponse { Message = SameMessageForAll };
    }

    public async Task<AdminResetPasswordResponse> ResetPasswordAsync(
        AdminResetPasswordRequest request,
        CancellationToken ct)
    {
        var code = request.Code?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return new AdminResetPasswordResponse { Success = false, Message = "Invalid or expired code." };

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return new AdminResetPasswordResponse { Success = false, Message = "Password must be at least 8 characters." };

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            return new AdminResetPasswordResponse { Success = false, Message = "Passwords do not match." };

        var cacheKey = $"forgotpwd:code:{code}";
        if (!cache.TryGetValue(cacheKey, out int userId))
            return new AdminResetPasswordResponse { Success = false, Message = "Invalid or expired code." };

        cache.Remove(cacheKey);

        var credential = await credentialQueryService.GetByUserId(userId, ct);
        if (credential is null)
            return new AdminResetPasswordResponse { Success = false, Message = "Invalid or expired code." };

        var user = await userQueryService.GetById(userId, ct)
                   ?? throw new InvalidOperationException("User not found for credential.");

        var newHash = passwordHasher.HashPassword(user, request.NewPassword);
        credential.PasswordHash = newHash;
        credential.PasswordUpdatedAt = DateTime.UtcNow;
        credential.FailedCount = 0;
        credential.LockoutUntilUtc = null;
        await credentialCommandService.Update(credential, saveChanges: true, ct);

        try
        {
            await appNotificationFacade.UserPasswordChanged(
                user.Id,
                user.DisplayName ?? user.Email ?? "",
                credential.Login,
                "reset-password (one-time code)",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify admins about password reset for user {UserId}", user.Id);
        }

        return new AdminResetPasswordResponse { Success = true, Message = "Password has been reset." };
    }

    private bool IsRateLimited(string cacheKey, out int retryAfterSeconds)
    {
        retryAfterSeconds = RateLimitWindowMinutes * 60;
        var lockObj = RateLimitLocks.GetOrAdd(cacheKey, _ => new object());
        lock (lockObj)
        {
            if (!cache.TryGetValue(cacheKey, out RateLimitEntry? entry) || entry is null)
                return false;
            var now = DateTimeOffset.UtcNow;
            if (now - entry.FirstRequestUtc > TimeSpan.FromMinutes(RateLimitWindowMinutes))
                return false;
            if (entry.Count >= RateLimitMaxRequests)
                return true;
            return false;
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
            cache.Set(cacheKey, new RateLimitEntry(entry.FirstRequestUtc, entry.Count + 1), TimeSpan.FromMinutes(RateLimitWindowMinutes + 1));
        }
    }

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

    private sealed record RateLimitEntry(DateTimeOffset FirstRequestUtc, int Count);
}
