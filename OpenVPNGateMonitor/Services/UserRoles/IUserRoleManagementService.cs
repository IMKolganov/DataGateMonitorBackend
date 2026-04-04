using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.UserRoles;

/// <summary>
/// Admin API: catalog of roles and replacing a user's assigned role.
/// </summary>
public interface IUserRoleManagementService
{
    Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default);

    /// <summary>Returns the user's current role row, or null if none.</summary>
    Task<(UserRole UserRole, Role Role)?> GetAssignmentByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>Replaces all role rows for the user with a single role.</summary>
    Task<(UserRole UserRole, Role Role)> SetUserRoleAsync(int userId, int roleId, CancellationToken ct = default);
}
