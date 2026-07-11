using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Api.Auth.Users;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.User;

namespace DataGateMonitor.Services.Api.Auth.Registers;

public sealed class UserRegistrationService(
    IPasswordHasher<User> passwordHasher,
    ICommandService<UserCredential, int> userCredentialCommandService,
    ICommandService<UserIdentityLink, int> userIdentityLinkCommandService,
    IUserCredentialQueryService userCredentialQueryService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    IUserQueryService userQueryService,
    IUserAccountService userAccountService,
    IEmailConfirmationService emailConfirmationService,
    ISettingsService settingsService,
    IAppNotificationFacade appNotificationFacade,
    IUserPasswordHistoryService passwordHistoryService,
    ILogger<UserRegistrationService> logger
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
            var emailTaken = await userQueryService.AnyByEmail(email, ct);

            if (emailTaken)
                throw new InvalidOperationException("Email is already in use.");
        }

        var requireEmailConfirmation = await IsEmailConfirmationRequiredAsync(ct);
        var user = UserFactory.CreateNew(
            displayName!,
            email,
            isEmailConfirmed: string.IsNullOrWhiteSpace(email) || !requireEmailConfirmation);

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

        await userCredentialCommandService.Add(credential, saveChanges: true, ct);

        await passwordHistoryService.RecordSnapshotBeforeChangeAsync(
            credential,
            PasswordSetActorKind.System,
            null,
            "registration",
            ct);

        await LocalUserIdentityLinkEnsurer.EnsureAsync(
            user.Id,
            userIdentityLinkQueryService,
            userIdentityLinkCommandService,
            ct);

        if (requireEmailConfirmation && !string.IsNullOrWhiteSpace(user.Email))
            await emailConfirmationService.SendConfirmationAsync(user.Id, user.Email, ct);

        try
        {
            await appNotificationFacade.UserRegistered(user.Id, user.DisplayName ?? "", login, user.Email, "password", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify admins about new user {UserId}", user.Id);
        }

        return new RegisterUserResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName ?? "",
            Email = user.Email,
            HasDashboardAccess = user.HasDashboardAccess
        };
    }

    private async Task<bool> IsEmailConfirmationRequiredAsync(CancellationToken ct)
    {
        var typeKey = $"{AuthEmailSettingsKeys.RequireEmailConfirmationOnRegister}_Type";
        var type = await settingsService.GetValueAsync<string>(typeKey, ct);
        if (!string.Equals(type, "bool", StringComparison.OrdinalIgnoreCase))
            return true;

        var value = await settingsService.GetValueAsync<bool>(AuthEmailSettingsKeys.RequireEmailConfirmationOnRegister, ct);
        return value;
    }
}
