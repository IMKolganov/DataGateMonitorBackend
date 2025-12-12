using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

public interface IUserRoleService
{
    Task<UserRole> AssignRoleAsync(int userId, int roleId, CancellationToken ct);
    Task<string> GetUserRoleNameAsync(int userId, CancellationToken ct);
}