using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.RoleTable;

public class RoleQueryService(
    IQueryService<Role, int> q) : IRoleQueryService
{
    public Task<List<Role>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<Role?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<IPagedResult<Role>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);

    public Task<List<Role>> SearchAsync(
        Expression<Func<Role, bool>> predicate,
        CancellationToken ct)
        => q.WhereAsync(predicate, ct: ct);
}