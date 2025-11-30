using Microsoft.AspNetCore.Identity;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Auth.Models;

namespace OpenVPNGateMonitor.Services.Auth;


public sealed class UserAuthService(
    IUnitOfWork uow,
    IUserCredentialQueryService userCredentialQueryService,
    IQueryService<User, int> usersQ,
    ICommandService<UserCredential, int> credsCmd) : IUserAuthService
{
    private readonly PasswordHasher<object> _hasher = new();

    public async Task<UserAuthResult> VerifyAsync(string login, string password, CancellationToken ct)
    {
        var normalizedLogin = login.Trim().ToUpperInvariant();

        // Get credential by normalized login
        var userCredential = await userCredentialQueryService.GetByNormalizedLogin(normalizedLogin, ct);

        if (userCredential is null)
            return UserAuthResult.Fail("not_found");

        // Lockout check
        if (userCredential.LockoutUntilUtc.HasValue && userCredential.LockoutUntilUtc.Value > DateTime.UtcNow)
            return UserAuthResult.Fail("locked");

        var verify = _hasher.VerifyHashedPassword(new object(), userCredential.PasswordHash, password);

        if (verify == PasswordVerificationResult.Failed)
        {
            // Increment counters (partial update)
            userCredential.FailedCount++;
            if (userCredential.FailedCount >= 5)
            {
                userCredential.LockoutUntilUtc = DateTime.UtcNow.AddMinutes(15);
                userCredential.FailedCount = 0;
            }

            await credsCmd.UpdateAsync(userCredential, saveChanges: false, ct);
            await uow.SaveChangesAsync(ct);
            return UserAuthResult.Fail("invalid");
        }

        // Success: reset counters
        userCredential.FailedCount = 0;
        userCredential.LockoutUntilUtc = null;

        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            userCredential.PasswordHash = _hasher.HashPassword(new object(), password);
            userCredential.PasswordUpdatedAt = DateTime.UtcNow;
        }

        await credsCmd.UpdateAsync(userCredential, saveChanges: false, ct);
        await uow.SaveChangesAsync(ct);

        // Optional: ensure user is not blocked
        var user = await usersQ.FindByIdAsync(userCredential.UserId, ct: ct);
        if (user is null || user.IsBlocked)
            return UserAuthResult.Fail("forbidden");

        return UserAuthResult.Success(userCredential.UserId);
    }
}