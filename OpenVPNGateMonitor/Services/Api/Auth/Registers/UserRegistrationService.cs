using Microsoft.AspNetCore.Identity;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.Api.Auth.Users;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers;

public sealed class UserRegistrationService(
    IPasswordHasher<User> passwordHasher,
    ICommandService<UserCredential, int> userCredentialCommandService,
    IUserCredentialQueryService userCredentialQueryService,
    IUserQueryService userQueryService,
    IUserAccountService userAccountService
) : IUserRegistrationService
{
    public async Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken ct)
    {
        var displayName = request.DisplayName?.Trim();
        var login = request.Login?.Trim();
        var email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.");

        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Login is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            throw new ArgumentException("Passwords do not match.");

        if (request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long.");

        var normalizedLogin = login.ToUpperInvariant();

        var existingCredential = await userCredentialQueryService
            .GetByNormalizedLogin(normalizedLogin, ct);

        if (existingCredential is not null)
            throw new InvalidOperationException("Login is already in use.");

        if (!string.IsNullOrEmpty(email))
        {
            var emailTaken = await userQueryService.AnyByEmailAsync(email, ct);

            if (emailTaken)
                throw new InvalidOperationException("Email is already in use.");
        }

        var user = UserFactory.CreateNew(displayName!, email);

        user = await userAccountService.CreateUserWithDefaultRoleAsync(user, ct);

        var passwordHash = passwordHasher.HashPassword(user, request.Password);

        var credential = new UserCredential
        {
            UserId = user.Id,
            Login = login!,
            NormalizedLogin = normalizedLogin,
            PasswordHash = passwordHash,
            PasswordAlgo = "AspNetCoreV3",
            PasswordUpdatedAt = DateTime.UtcNow,
            FailedCount = 0,
            LockoutUntilUtc = null
        };

        await userCredentialCommandService.AddAsync(credential, saveChanges: true, ct);

        return new RegisterUserResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            HasDashboardAccess = user.HasDashboardAccess
        };
    }
}
