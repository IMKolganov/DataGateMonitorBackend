using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation.Models;

namespace DataGateMonitor.Services.Api.Auth.EmailConfirmation;

public sealed class EmailConfirmationService(
    IMemoryCache cache,
    IUserQueryService userQueryService,
    ICommandService<User, int> userCommandService,
    IEmailSenderService emailSenderService) : IEmailConfirmationService
{
    private const int CodeLength = 6;
    private const int CodeTtlMinutes = 30;
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

        var code = GenerateCode();
        cache.Set(
            key: GetCodeCacheKey(normalizedEmail),
            value: new EmailConfirmationCodePayload(userId, code),
            options: new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CodeTtlMinutes)
            });

        var subject = "Confirm your DataGate email";
        var body = BuildEmailConfirmationHtml(code);
        await emailSenderService.SendAsync(email, subject, body, ct);
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

    private static string BuildEmailConfirmationHtml(string code)
    {
        var safeCode = WebUtility.HtmlEncode(code);
        return $"""
                <!doctype html>
                <html lang="en">
                <head>
                  <meta charset="UTF-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1.0">
                  <title>DataGate Email Confirmation</title>
                </head>
                <body style="margin:0;padding:0;background:#0d1117;color:#c9d1d9;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#0d1117;padding:24px 12px;">
                    <tr>
                      <td align="center">
                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:640px;background:linear-gradient(180deg,rgba(18,23,30,0.96),rgba(13,17,23,0.99));border:1px solid #30363d;border-radius:14px;overflow:hidden;">
                          <tr>
                            <td style="padding:28px 28px 12px 28px;">
                              <div style="display:inline-block;padding:6px 12px;border-radius:999px;background:rgba(56,139,253,0.14);border:1px solid rgba(88,166,255,0.24);color:#9ecbff;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;">DataGate</div>
                              <h1 style="margin:16px 0 8px 0;font-size:28px;line-height:1.2;color:#ffffff;">Confirm your email</h1>
                              <p style="margin:0;color:#8b949e;font-size:15px;line-height:1.6;">
                                Use the confirmation code below to finish registration in DataGate.
                              </p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:8px 28px 22px 28px;">
                              <div style="background:#161b22;border:1px solid #30363d;border-radius:12px;padding:18px;text-align:center;">
                                <div style="font-size:12px;color:#8b949e;letter-spacing:.08em;text-transform:uppercase;margin-bottom:8px;">Confirmation code</div>
                                <div style="font-size:34px;font-weight:700;letter-spacing:6px;color:#58a6ff;">{safeCode}</div>
                                <div style="margin-top:10px;font-size:13px;color:#8b949e;">Valid for {CodeTtlMinutes} minutes</div>
                              </div>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:0 28px 18px 28px;">
                              <div style="background:#161b22;border:1px solid #30363d;border-radius:12px;padding:16px;">
                                <p style="margin:0 0 8px 0;color:#c9d1d9;font-size:14px;font-weight:600;">Get DataGate clients</p>
                                <p style="margin:0 0 10px 0;color:#8b949e;font-size:14px;">Download all available clients here:</p>
                                <a href="https://datagateapp.com/download" style="color:#58a6ff;text-decoration:none;">https://datagateapp.com/download</a>
                              </div>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:0 28px 28px 28px;">
                              <div style="background:#161b22;border:1px solid #30363d;border-radius:12px;padding:16px;">
                                <p style="margin:0 0 8px 0;color:#c9d1d9;font-size:14px;font-weight:600;">Follow our Telegram channel</p>
                                <a href="https://t.me/datagateapp" style="color:#58a6ff;text-decoration:none;">https://t.me/datagateapp</a>
                              </div>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:0 28px 28px 28px;">
                              <p style="margin:0;color:#6e7681;font-size:12px;line-height:1.6;">
                                If you did not request this email, you can safely ignore it.<br>
                                DataGate Team
                              </p>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
    }
}
