using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Api.Auth.Totp;
using DataGateMonitor.Services.Api.Auth.Users;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.Login;

public sealed class UserLoginService(
    IUserCredentialQueryService credentialQueryService,
    IUserQueryService userQueryService,
    IPasswordHasher<User> passwordHasher,
    IGoogleTokenValidator tokenValidator,
    ICommandService<UserIdentityLink, int> userIdentityLinkCommandService,
    ICommandService<User, int> userCommandService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IUserAccountService userAccountService,
    ITokenService tokenService,
    IAdminTotpService adminTotpService,
    IAppNotificationFacade appNotificationFacade,
    ILogger<UserLoginService> logger,
    IHttpContextAccessor httpContextAccessor
) : IUserLoginService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var login = request.Login?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Login is required.");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.");

        var normalizedLogin = login.ToUpperInvariant();

        var credential = await credentialQueryService.GetByNormalizedLogin(normalizedLogin, ct);
        if (credential is null && login.Contains('@', StringComparison.Ordinal))
        {
            var userByEmail = await userQueryService.GetByEmail(login, ct);
            if (userByEmail is not null)
                credential = await credentialQueryService.GetByUserId(userByEmail.Id, ct);
        }

        if (credential is null)
            throw new UnauthorizedAccessException("Invalid login or password.");

        var user = await userQueryService.GetById(credential.UserId, ct)
                   ?? throw new InvalidOperationException("User record is missing.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("User account is blocked.");

        if (!string.IsNullOrWhiteSpace(user.Email) && !user.IsEmailConfirmed)
            throw new UnauthorizedAccessException(
                "Email is not confirmed. Request a confirmation code and verify your email first.");

        var result = passwordHasher.VerifyHashedPassword(user, credential.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid login or password.");

        await LocalUserIdentityLinkEnsurer.EnsureAsync(
            user.Id,
            userIdentityLinkQueryService,
            userIdentityLinkCommandService,
            ct);

        var (deviceId, userAgent) = GetClientInfo();

        return await adminTotpService.ApplyAdminTotpGateAsync(
            user,
            credential,
            externalId: null,
            deviceId,
            userAgent,
            async cancel =>
            {
                var tokenPair = await tokenService.IssueAsync(
                    userId: user.Id,
                    externalId: null,
                    deviceId: deviceId,
                    userAgent: userAgent,
                    ct: cancel);

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
            },
            ct);
    }

    public async Task<GoogleLoginResponse> LoginWithGoogleAsync(string idToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("IdToken is required.", nameof(idToken));

        var googleUser = await tokenValidator.ValidateAsync(idToken, ct);

        if (string.IsNullOrWhiteSpace(googleUser.Subject))
            throw new InvalidOperationException("Invalid Google token: subject is missing.");

        const string provider = "google";
        var externalId = googleUser.Subject;

        var existingLink = await userIdentityLinkQueryService
            .GetByProviderAndExternalId(provider, externalId, ct);

        User? user = null;
        var isNew = false;

        if (existingLink is { UserId: > 0 })
        {
            user = await userQueryService.GetById(existingLink.UserId, ct)
                   ?? throw new InvalidOperationException("User linked to Google account not found.");
        }
        else
        {
            if (!string.IsNullOrEmpty(googleUser.Email))
                user = await userQueryService.GetByEmail(googleUser.Email, ct);

            if (user is null)
            {
                var displayName = googleUser.Name ?? googleUser.Email ?? "Google User";
                var newUser = UserFactory.CreateNew(displayName, googleUser.Email, isEmailConfirmed: true);
                user = await userAccountService.CreateUserWithDefaultRoleAsync(newUser, ct);
                isNew = true;
            }

            var link = new UserIdentityLink
            {
                UserId = user.Id,
                Provider = provider,
                ExternalId = externalId
            };

            await userIdentityLinkCommandService.Add(link, saveChanges: true, ct);
        }

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("User account is blocked.");

        if (!string.IsNullOrWhiteSpace(googleUser.Email)
            && string.Equals(user.Email, googleUser.Email, StringComparison.OrdinalIgnoreCase)
            && !user.IsEmailConfirmed)
        {
            user.IsEmailConfirmed = true;
            await userCommandService.Update(user, saveChanges: true, ct);
        }

        var normalizedPicture = NormalizeGoogleProfilePictureUrl(googleUser.Picture);
        if (!string.IsNullOrEmpty(normalizedPicture)
            && !string.Equals(user.AvatarUrl ?? "", normalizedPicture, StringComparison.Ordinal))
        {
            user.AvatarUrl = normalizedPicture;
            await userCommandService.Update(user, saveChanges: true, ct);
        }

        if (isNew)
        {
            try
            {
                await appNotificationFacade.UserRegistered(
                    user.Id,
                    user.DisplayName ?? "",
                    login: null,
                    email: user.Email,
                    registrationSource: "Google",
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to notify admins about new Google user {UserId}", user.Id);
            }
        }

        var (deviceId, userAgent) = GetClientInfo();
        var credential = await credentialQueryService.GetByUserId(user.Id, ct);

        var loginResult = await adminTotpService.ApplyAdminTotpGateAsync(
            user,
            credential,
            externalId,
            deviceId,
            userAgent,
            async cancel =>
            {
                var tokenPair = await tokenService.IssueAsync(
                    userId: user.Id,
                    externalId: externalId,
                    deviceId: deviceId,
                    userAgent: userAgent,
                    ct: cancel);

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
            },
            ct);

        return new GoogleLoginResponse
        {
            Token = loginResult.Token,
            Expiration = loginResult.Expiration,
            RefreshToken = loginResult.RefreshToken,
            RefreshExpiration = loginResult.RefreshExpiration,
            UserId = loginResult.UserId,
            DisplayName = loginResult.DisplayName,
            Email = loginResult.Email,
            RequiresTotp = loginResult.RequiresTotp,
            LoginChallengeId = loginResult.LoginChallengeId,
            RequiresTotpSetup = loginResult.RequiresTotpSetup,
            IsNewUser = isNew,
            AvatarUrl = user.AvatarUrl,
        };
    }

    /// <summary>Google profile images are HTTPS URLs; reject anything else.</summary>
    private static string? NormalizeGoogleProfilePictureUrl(string? picture)
    {
        if (string.IsNullOrWhiteSpace(picture))
            return null;

        var t = picture.Trim();
        if (t.Length > 2048)
            t = t[..2048];

        if (!t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return null;

        return t;
    }

    private (string? DeviceId, string? UserAgent) GetClientInfo()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null)
            return (null, null);

        var userAgent = ctx.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
            userAgent = null;

        var deviceId = ctx.Request.Headers["X-Device-Id"].ToString();
        if (string.IsNullOrWhiteSpace(deviceId))
            deviceId = null;

        return (deviceId, userAgent);
    }
}
