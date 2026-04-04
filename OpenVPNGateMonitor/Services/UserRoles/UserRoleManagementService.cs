using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.RoleTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Auth;

namespace OpenVPNGateMonitor.Services.UserRoles;

public sealed class UserRoleManagementService(
    ILogger<UserRoleManagementService> logger,
    ICommandService<UserRole, int> userRoleCommandService,
    IUserRoleQueryService userRoleQueryService,
    IRoleQueryService roleQueryService,
    IUserQueryService userQueryService) : IUserRoleManagementService
{
    public Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        => roleQueryService.GetAll(ct);

    public async Task<(UserRole UserRole, Role Role)?> GetAssignmentByUserIdAsync(int userId,
        CancellationToken ct = default)
    {
        _ = await userQueryService.GetById(userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var ur = await userRoleQueryService.GetByUserId(userId, ct);
        if (ur is null)
            return null;

        var role = await roleQueryService.GetById(ur.RoleId, ct)
                   ?? throw new InvalidOperationException($"Role {ur.RoleId} not found.");

        return (ur, role);
    }

    public async Task<(UserRole UserRole, Role Role)> SetUserRoleAsync(int userId, int roleId,
        CancellationToken ct = default)
    {
        _ = await userQueryService.GetById(userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var role = await roleQueryService.GetById(roleId, ct)
                   ?? throw new KeyNotFoundException($"Role {roleId} not found.");

        if (roleId == SystemRoles.ServiceId)
            throw new InvalidOperationException("The Service role cannot be assigned through this API.");

        var existing = await userRoleQueryService.GetByUserId(userId, ct);
        if (existing?.RoleId == SystemRoles.AdminId && roleId != SystemRoles.AdminId)
        {
            var adminIds = await userRoleQueryService.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, ct);
            if (adminIds.Count == 1 && adminIds[0] == userId)
                throw new InvalidOperationException("Cannot remove the last administrator.");
        }

        if (existing is not null && existing.RoleId == roleId)
        {
            logger.LogInformation("User {UserId} already has role {RoleId}; no change.", userId, roleId);
            return (existing, role);
        }

        await userRoleCommandService.DeleteWhere(ur => ur.UserId == userId, ct);

        var now = DateTimeOffset.UtcNow;
        var created = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            CreateDate = now,
            LastUpdate = now
        };

        created = await userRoleCommandService.Add(created, saveChanges: true, ct);
        logger.LogInformation("User {UserId} assigned role {RoleId} ({RoleName}).", userId, role.Id, role.Name);

        return (created, role);
    }
}
