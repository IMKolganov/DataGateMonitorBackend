using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
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
    IUserRoleService userRoleService,
    IConfiguration configuration,
    IGoogleTokenValidator tokenValidator,
    ICommandService<UserIdentityLink, int> userIdentityLinkCommandService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IUserAccountService userAccountService
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

        var (token, expires) = await CreateJwtAsync(user, null, ct);

        return new LoginResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Token = token,
            Expiration = expires
        };
    }
    
    public async Task<GoogleLoginResponse> LoginWithGoogleAsync(string idToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("IdToken is required.", nameof(idToken));

        var googleUser = await tokenValidator.ValidateAsync(idToken, ct);

        if (string.IsNullOrWhiteSpace(googleUser.Subject))
            throw new InvalidOperationException("Invalid Google token: subject is missing.");

        var provider = "google";
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

        var (token, expires) = await CreateJwtAsync(user, externalId, ct);

        return new GoogleLoginResponse
        {
            Token = token,
            Expiration = expires,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsNewUser = isNew
        };
    }


    private async Task<(string Token, DateTimeOffset Expires)> CreateJwtAsync(User user, string? externalId,
        CancellationToken ct)
    {
        var secret = configuration["Jwt:Secret"]
                     ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(1);

        var role = await userRoleService.GetUserRoleNameAsync(user.Id, ct);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName ?? string.Empty),
            new(ClaimTypes.Role, role),
            new("externalId", externalId ?? string.Empty),
            new(JwtRegisteredClaimNames.Sub, externalId ?? string.Empty),

            new("displayName", user.DisplayName ?? string.Empty),
            new("email", user.Email ?? string.Empty),
        };
        
        var tokenDescriptor = new JwtSecurityToken(
            issuer: "OpenVPNGateBackend",
            audience: "OpenVPNGateFrontend",
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(tokenDescriptor);

        return (token, expires);
    }
}