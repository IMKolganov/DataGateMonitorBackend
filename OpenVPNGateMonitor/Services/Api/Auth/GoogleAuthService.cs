using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Api.Auth.Users;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth;

public sealed class GoogleAuthService(
    IGoogleTokenValidator tokenValidator,
    ICommandService<UserIdentityLink, int> userIdentityLinkCommandService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IUserQueryService userQueryService,
    IUserAccountService userAccountService,
    IConfiguration configuration
) : IGoogleAuthService
{
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
            .GetByProviderAndExternalIdAsync(provider, externalId, ct);

        User? user = null;
        var isNew = false;

        if (existingLink is { UserId: > 0 })
        {
            user = await userQueryService.GetByIdAsync(existingLink.UserId, ct)
                   ?? throw new InvalidOperationException("User linked to Google account not found.");
        }
        else
        {
            if (!string.IsNullOrEmpty(googleUser.Email))
                user = await userQueryService.GetByEmailAsync(googleUser.Email, ct);

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

            await userIdentityLinkCommandService.AddAsync(link, saveChanges: true, ct);
        }

        var (token, expires) = CreateJwtForUser(user);

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

    private (string token, DateTimeOffset expires) CreateJwtForUser(User user)
    {
        var secret = configuration["Jwt:Secret"] 
                     ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddHours(1);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: "OpenVPNGateBackend",
            audience: "OpenVPNGateFrontend",
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(tokenDescriptor);

        return (token, expires);
    }
}
