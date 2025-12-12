using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.RoleTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth;

public class UserRoleService(IUserRoleQueryService userRoleQueryService, IRoleQueryService roleQueryService, 
    ICommandService<UserRole, int> userRoleCommandService) : IUserRoleService
{
    public async Task<UserRole> AssignRoleAsync(int userId, int roleId, CancellationToken ct)
    {
        var exists = await userRoleQueryService.GetByIdAndUserIdAsync(roleId, userId, ct);

        if (exists is not null)
            return exists;

        var userRole = new UserRole()
        {
            UserId = userId,
            RoleId = roleId
        };

        userRole = await userRoleCommandService.AddAsync(userRole, true, ct);

        return userRole;
    }

    public async Task<string> GetUserRoleNameAsync(int userId, CancellationToken ct)
    {
        var role = await userRoleQueryService.GetByUserIdAsync(userId, ct);

        if (role == null || role.RoleId <= 0)
            throw new Exception($"Role not found for user {userId}");

        var roleName = await roleQueryService.GetByIdAsync(role.RoleId, ct);
        
        if (roleName is null)
            throw new Exception($"Role not found for role {role.RoleId}");

        return roleName.Name ?? throw new Exception($"Role name not found for id {role.RoleId}");
    }

}