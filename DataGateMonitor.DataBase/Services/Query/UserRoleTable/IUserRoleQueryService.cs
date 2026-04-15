using System.Linq.Expressions;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.UserRoleTable;

public interface IUserRoleQueryService
{
    Task<List<UserRole>> GetAll(CancellationToken ct);
    Task<UserRole?> GetById(int id, CancellationToken ct);
    Task<UserRole?> GetByUserId(int userId, CancellationToken ct);
    Task<UserRole?> GetByIdAndUserId(int id, int userId, CancellationToken ct);
    Task<IPagedResult<UserRole>> GetPage(int page, int pageSize, CancellationToken ct);
    public Task<List<UserRole>> Search(
        Expression<Func<UserRole, bool>> predicate,
        CancellationToken ct);

    /// <summary>
    /// Returns list of UserIds that have the given role (e.g. dashboard admins).
    /// </summary>
    Task<List<int>> GetUserIdsByRoleIdAsync(int roleId, CancellationToken ct = default);
}