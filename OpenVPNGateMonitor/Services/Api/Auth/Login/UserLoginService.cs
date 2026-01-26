using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.Api.Auth.Users;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Login;

public sealed class UserLoginService(
    IUserCredentialQueryService credentialQueryService,
    IUserQueryService userQueryService,
    IPasswordHasher<User> passwordHasher,
    IGoogleTokenValidator tokenValidator,
    ICommandService<UserIdentityLink, int> userIdentityLinkCommandService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IUserAccountService userAccountService,
    ITokenService tokenService,
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
        if (credential is null)
            throw new UnauthorizedAccessException("Invalid login or password.");

        var user = await userQueryService.GetById(credential.UserId, ct)
                   ?? throw new InvalidOperationException("User record is missing.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("User account is blocked.");

        var result = passwordHasher.VerifyHashedPassword(user, credential.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid login or password.");

        var (deviceId, userAgent) = GetClientInfo();

        var tokenPair = await tokenService.IssueAsync(
            userId: user.Id,
            externalId: null,
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
                var newUser = UserFactory.CreateNew(displayName, googleUser.Email);
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

        var (deviceId, userAgent) = GetClientInfo();

        var tokenPair = await tokenService.IssueAsync(
            userId: user.Id,
            externalId: externalId,
            deviceId: deviceId,
            userAgent: userAgent,
            ct: ct);

        return new GoogleLoginResponse
        {
            Token = tokenPair.AccessToken,
            Expiration = tokenPair.AccessExpiresAt,
            RefreshToken = tokenPair.RefreshToken,
            RefreshExpiration = tokenPair.RefreshExpiresAt,
            UserId = user.Id
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsNewUser = isNew
        };
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
