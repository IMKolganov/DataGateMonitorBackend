using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IUserRoleService
{
    Task<UserRole> AssignRoleAsync(int userId, int roleId, CancellationToken ct);
    Task<string> GetUserRoleNameAsync(int userId, CancellationToken ct);
}