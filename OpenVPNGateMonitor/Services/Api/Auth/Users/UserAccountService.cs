using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.SharedModels.Auth;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth.Users;

public sealed class UserAccountService(
    ICommandService<User, int> userCommandService,
    IUserRoleService userRoleService)
    : IUserAccountService
{
    public async Task<User> CreateUserWithDefaultRoleAsync(User user, CancellationToken ct)
    {
        user = await userCommandService.AddAsync(user, saveChanges: true, ct);

        if (user.Id <= 0)
            throw new InvalidOperationException($"Failed to create user {user.DisplayName}");

        await userRoleService.AssignRoleAsync(user.Id, SystemRoles.VpnUserId, ct);

        return user;
    }
}
