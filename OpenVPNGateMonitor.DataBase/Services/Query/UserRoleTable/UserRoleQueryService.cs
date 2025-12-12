using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;

public class UserRoleQueryService(
    IQueryService<UserRole, int> q) : IUserRoleQueryService
{
    public Task<List<UserRole>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<UserRole?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<UserRole?> GetByUserIdAsync(int userId, CancellationToken ct)
        => q.FirstOrDefaultAsync(x=> x.UserId == userId, ct: ct);

    public Task<UserRole?> GetByIdAndUserIdAsync(int id, int userId, CancellationToken ct)
        => q.FirstOrDefaultAsync(x=> x.Id == id && x.UserId == userId, ct: ct);

    public Task<IPagedResult<UserRole>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);

    public Task<List<UserRole>> SearchAsync(
        Expression<Func<UserRole, bool>> predicate,
        CancellationToken ct)
        => q.WhereAsync(predicate, ct: ct);
}