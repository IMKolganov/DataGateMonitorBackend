using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.RoleTable;
using DataGateMonitor.DataBase.Services.Query.UserRoleTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace DataGateMonitor.Services.Api.Auth.Registers;

public class UserRoleService(IUserRoleQueryService userRoleQueryService, IRoleQueryService roleQueryService, 
    ICommandService<UserRole, int> userRoleCommandService) : IUserRoleService
{
    public async Task<UserRole> AssignRoleAsync(int userId, int roleId, CancellationToken ct)
    {
        var exists = await userRoleQueryService.GetByIdAndUserId(roleId, userId, ct);

        if (exists is not null)
            return exists;

        var userRole = new UserRole()
        {
            UserId = userId,
            RoleId = roleId
        };

        userRole = await userRoleCommandService.Add(userRole, true, ct);

        return userRole;
    }

    public async Task<string> GetUserRoleNameAsync(int userId, CancellationToken ct)
    {
        var role = await userRoleQueryService.GetByUserId(userId, ct);

        if (role == null || role.RoleId <= 0)
            throw new Exception($"Role not found for user {userId}");

        var roleName = await roleQueryService.GetById(role.RoleId, ct);
        
        if (roleName is null)
            throw new Exception($"Role not found for role {role.RoleId}");

        return roleName.Name ?? throw new Exception($"Role name not found for id {role.RoleId}");
    }

}