using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;

public interface IUserRoleQueryService
{
    Task<List<UserRole>> GetAllAsync(CancellationToken ct);
    Task<UserRole?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserRole?> GetByUserIdAsync(int userId, CancellationToken ct);
    Task<UserRole?> GetByIdAndUserIdAsync(int id, int userId, CancellationToken ct);
    Task<IPagedResult<UserRole>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    public Task<List<UserRole>> SearchAsync(
        Expression<Func<UserRole, bool>> predicate,
        CancellationToken ct);
}