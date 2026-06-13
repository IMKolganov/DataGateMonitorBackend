using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using OtpNet;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.Totp;

public sealed class AdminTotpService(
    IUserRoleService userRoleService,
    IUserCredentialQueryService credentialQueryService,
    ICommandService<UserCredential, int> credentialCommandService,
    ITokenService tokenService,
    IPasswordHasher<User> passwordHasher,
    IUserQueryService userQueryService,
    IMemoryCache memoryCache,
    IConfiguration configuration) : IAdminTotpService
{
    private const string AdminRole = "Admin";
    private const string Issuer = "DataGate Monitor";
    /// <summary>Placeholder credential for admins who sign in via Google/OAuth (no password login).</summary>
    private const string ExternalAuthOnlyPasswordAlgo = "ExternalAuthOnly";
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SetupSecretLifetime = TimeSpan.FromMinutes(10);
    private const int MaxChallengeAttempts = 5;

    public async Task<bool> IsAdminUserAsync(int userId, CancellationToken ct)
    {
        var role = await userRoleService.GetUserRoleNameAsync(userId, ct);
        return string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsTotpEnabled(UserCredential? credential) =>
        credential?.TotpEnabledAt != null && !string.IsNullOrWhiteSpace(credential.TotpSecretEncrypted);

    public async Task<LoginResponse> ApplyAdminTotpGateAsync(
        User user,
        UserCredential? credential,
        string? externalId,
        string? deviceId,
        string? userAgent,
        Func<CancellationToken, Task<LoginResponse>> issueTokensAsync,
        CancellationToken ct)
    {
        if (!await IsAdminUserAsync(user.Id, ct))
            return await issueTokensAsync(ct);

        if (IsTotpEnabled(credential))
        {
            var challengeId = CreateLoginChallenge(user.Id, externalId, deviceId, userAgent);
            return new LoginResponse
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                RequiresTotp = true,
                LoginChallengeId = challengeId,
            };
        }

        var tokens = await issueTokensAsync(ct);
        tokens.RequiresTotpSetup = true;
        return tokens;
    }

    public async Task<LoginResponse> VerifyLoginChallengeAsync(TotpVerifyLoginRequest request, CancellationToken ct)
    {
        var challengeId = request.LoginChallengeId?.Trim();
        var code = request.Code?.Trim();
        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code))
            throw new UnauthorizedAccessException("Invalid verification code.");

        if (!memoryCache.TryGetValue(ChallengeCacheKey(challengeId), out AdminLoginChallenge? challenge)
            || challenge is null
            || challenge.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Login challenge expired. Sign in again.");
        }

        var user = await userQueryService.GetById(challenge.UserId, ct)
                   ?? throw new UnauthorizedAccessException("User not found.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("User account is blocked.");

        var credential = await credentialQueryService.GetByUserId(user.Id, ct);
        if (!IsTotpEnabled(credential))
        {
            memoryCache.Remove(ChallengeCacheKey(challengeId));
            throw new UnauthorizedAccessException("Two-factor authentication is not enabled.");
        }

        if (!VerifyCode(credential!.TotpSecretEncrypted!, code))
        {
            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= MaxChallengeAttempts)
            {
                memoryCache.Remove(ChallengeCacheKey(challengeId));
                throw new UnauthorizedAccessException("Too many invalid attempts. Sign in again.");
            }

            UpdateChallengeInCache(challengeId, challenge);
            throw new UnauthorizedAccessException("Invalid verification code.");
        }

        memoryCache.Remove(ChallengeCacheKey(challengeId));

        var tokenPair = await tokenService.IssueAsync(
            user.Id,
            challenge.ExternalId,
            challenge.DeviceId,
            challenge.UserAgent,
            ct);

        return new LoginResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Token = tokenPair.AccessToken,
            Expiration = tokenPair.AccessExpiresAt,
            RefreshToken = tokenPair.RefreshToken,
            RefreshExpiration = tokenPair.RefreshExpiresAt,
        };
    }

    public async Task<TotpStatusResponse> GetStatusAsync(int userId, CancellationToken ct)
    {
        var isAdmin = await IsAdminUserAsync(userId, ct);
        if (!isAdmin)
        {
            return new TotpStatusResponse
            {
                IsAdmin = false,
                TotpEnabled = false,
                RequiresTotpSetup = false,
            };
        }

        var credential = await credentialQueryService.GetByUserId(userId, ct);
        var enabled = IsTotpEnabled(credential);
        return new TotpStatusResponse
        {
            IsAdmin = true,
            TotpEnabled = enabled,
            RequiresTotpSetup = !enabled,
        };
    }

    public async Task<TotpSetupResponse> BeginSetupAsync(int userId, CancellationToken ct)
    {
        if (!await IsAdminUserAsync(userId, ct))
            throw new UnauthorizedAccessException("Two-factor setup is only available for administrators.");

        var user = await userQueryService.GetById(userId, ct)
                   ?? throw new InvalidOperationException("User not found.");

        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretBytes);

        memoryCache.Set(
            SetupCacheKey(userId),
            EncryptSecret(base32Secret),
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = SetupSecretLifetime });

        var accountName = string.IsNullOrWhiteSpace(user.Email)
            ? user.DisplayName
            : user.Email;

        var otpAuthUri = new OtpUri(OtpType.Totp, base32Secret, accountName, Issuer).ToString();

        return new TotpSetupResponse
        {
            SharedSecret = base32Secret,
            OtpAuthUri = otpAuthUri,
            Issuer = Issuer,
            AccountName = accountName ?? $"user-{userId}",
        };
    }

    public async Task ConfirmSetupAsync(int userId, TotpConfirmRequest request, CancellationToken ct)
    {
        if (!await IsAdminUserAsync(userId, ct))
            throw new UnauthorizedAccessException("Two-factor setup is only available for administrators.");

        if (!memoryCache.TryGetValue(SetupCacheKey(userId), out string? pendingEncrypted)
            || string.IsNullOrWhiteSpace(pendingEncrypted))
        {
            throw new InvalidOperationException("Setup session expired. Start setup again.");
        }

        if (!VerifyCode(pendingEncrypted, request.Code))
            throw new UnauthorizedAccessException("Invalid verification code.");

        var credential = await GetOrCreateCredentialForTotpAsync(userId, ct);

        credential.TotpSecretEncrypted = pendingEncrypted;
        credential.TotpEnabledAt = DateTimeOffset.UtcNow;
        await credentialCommandService.Update(credential, saveChanges: true, ct);
        memoryCache.Remove(SetupCacheKey(userId));
    }

    public async Task DisableAsync(int userId, TotpDisableRequest request, CancellationToken ct)
    {
        if (!await IsAdminUserAsync(userId, ct))
            throw new UnauthorizedAccessException("Two-factor is only managed for administrators.");

        var user = await userQueryService.GetById(userId, ct)
                   ?? throw new InvalidOperationException("User not found.");

        var credential = await credentialQueryService.GetByUserId(userId, ct)
                         ?? throw new InvalidOperationException("Two-factor is not configured.");

        if (!IsExternalAuthOnly(credential))
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required.");

            var verify = passwordHasher.VerifyHashedPassword(user, credential.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid password.");
        }

        if (!IsTotpEnabled(credential))
            return;

        if (!VerifyCode(credential.TotpSecretEncrypted!, request.Code))
            throw new UnauthorizedAccessException("Invalid verification code.");

        credential.TotpSecretEncrypted = null;
        credential.TotpEnabledAt = null;
        await credentialCommandService.Update(credential, saveChanges: true, ct);
    }

    private static bool IsExternalAuthOnly(UserCredential credential) =>
        string.Equals(credential.PasswordAlgo, ExternalAuthOnlyPasswordAlgo, StringComparison.Ordinal);

    private async Task<UserCredential> GetOrCreateCredentialForTotpAsync(int userId, CancellationToken ct)
    {
        var credential = await credentialQueryService.GetByUserId(userId, ct);
        if (credential is not null)
            return credential;

        var user = await userQueryService.GetById(userId, ct)
                   ?? throw new InvalidOperationException("User not found.");

        var login = await ResolveUniqueCredentialLoginAsync(user, ct);
        credential = new UserCredential
        {
            UserId = userId,
            Login = login,
            NormalizedLogin = login.ToUpperInvariant(),
            PasswordHash = passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N")),
            PasswordAlgo = ExternalAuthOnlyPasswordAlgo,
            PasswordUpdatedAt = DateTime.UtcNow,
            FailedCount = 0,
            LockoutUntilUtc = null,
        };

        await credentialCommandService.Add(credential, saveChanges: true, ct);
        return credential;
    }

    private async Task<string> ResolveUniqueCredentialLoginAsync(User user, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var email = user.Email.Trim();
            if (email.Length <= 128)
            {
                var taken = await credentialQueryService.GetByNormalizedLogin(email.ToUpperInvariant(), ct);
                if (taken is null)
                    return email;
            }
        }

        return $"admin-{user.Id}@totp.local";
    }

    private string CreateLoginChallenge(int userId, string? externalId, string? deviceId, string? userAgent)
    {
        var challengeId = Guid.NewGuid().ToString("N");
        memoryCache.Set(
            ChallengeCacheKey(challengeId),
            new AdminLoginChallenge
            {
                UserId = userId,
                ExternalId = externalId,
                DeviceId = deviceId,
                UserAgent = userAgent,
                ExpiresAtUtc = DateTimeOffset.UtcNow.Add(ChallengeLifetime),
            },
            new MemoryCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(ChallengeLifetime) });

        return challengeId;
    }

    private void UpdateChallengeInCache(string challengeId, AdminLoginChallenge challenge)
    {
        memoryCache.Set(
            ChallengeCacheKey(challengeId),
            challenge,
            new MemoryCacheEntryOptions { AbsoluteExpiration = challenge.ExpiresAtUtc });
    }

    private bool VerifyCode(string encryptedSecret, string code)
    {
        var secret = DecryptSecret(encryptedSecret);
        var totp = new OtpNet.Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(
            code.Trim().Replace(" ", "", StringComparison.Ordinal),
            out _,
            new VerificationWindow(previous: 1, future: 1));
    }

    private string EncryptSecret(string plainBase32)
    {
        var key = GetEncryptionKey();
        var plainBytes = Encoding.UTF8.GetBytes(plainBase32);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plainBytes, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    private string DecryptSecret(string encrypted)
    {
        var payload = Convert.FromBase64String(encrypted);
        if (payload.Length < 12 + 16)
            throw new InvalidOperationException("Invalid encrypted TOTP secret.");

        var nonce = payload.AsSpan(0, 12);
        var tag = payload.AsSpan(12, 16);
        var cipher = payload.AsSpan(28);
        var plain = new byte[cipher.Length];
        var key = GetEncryptionKey();
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    private byte[] GetEncryptionKey()
    {
        var pepper = configuration["Jwt:RefreshPepper"]
                     ?? configuration["Totp:EncryptionKey"]
                     ?? throw new InvalidOperationException("Jwt:RefreshPepper or Totp:EncryptionKey must be configured.");
        return SHA256.HashData(Encoding.UTF8.GetBytes(pepper));
    }

    private static string ChallengeCacheKey(string challengeId) => $"admin-totp-challenge:{challengeId}";
    private static string SetupCacheKey(int userId) => $"admin-totp-setup:{userId}";
}
