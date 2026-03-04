using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Auth;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth.Users;

public sealed class UserAccountService(
    ICommandService<User, int> userCommandService,
    IUserQuotaPlanService userQuotaPlanService,
    IQuotaPlanQueryService quotaPlanQueryService,
    IUserRoleService userRoleService)
    : IUserAccountService
{
    public async Task<User> CreateUserWithDefaultRoleAsync(User user, CancellationToken ct)
    {
        user = await userCommandService.Add(user, saveChanges: true, ct);

        if (user.Id <= 0)
            throw new InvalidOperationException($"Failed to create user {user.DisplayName}");

        await userRoleService.AssignRoleAsync(user.Id, SystemRoles.VpnUserId, ct);

        var quotaPlanDefault = await quotaPlanQueryService.GetDefault(ct);
        if (quotaPlanDefault != null) await userQuotaPlanService.AssignQuotaPlanAsync(user.Id, quotaPlanDefault.Id, ct);

        return user;
    }
}
