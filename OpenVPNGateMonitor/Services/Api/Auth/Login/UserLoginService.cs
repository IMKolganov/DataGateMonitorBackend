using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Login;

public sealed class UserLoginService(
    IUserCredentialQueryService credentialQueryService,
    IUserQueryService userQueryService,
    IPasswordHasher<User> passwordHasher,
    IUserRoleService userRoleService,
    IConfiguration configuration
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
            throw new InvalidOperationException("Invalid login or password.");

        var user = await userQueryService.GetByIdAsync(credential.UserId, ct)
                   ?? throw new InvalidOperationException("User record is missing.");

        if (user.IsBlocked)
            throw new InvalidOperationException("User account is blocked.");

        var result = passwordHasher.VerifyHashedPassword(user, credential.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Invalid login or password.");

        var (token, expires) = await CreateJwtAsync(user, ct);

        return new LoginResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Token = token,
            Expiration = expires
        };
    }


    private async Task<(string Token, DateTimeOffset Expires)> CreateJwtAsync(User user, CancellationToken ct)
    {
        var secret = configuration["Jwt:Secret"]
                     ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddHours(1);

        var role = await userRoleService.GetUserRoleNameAsync(user.Id, ct);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, role),
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