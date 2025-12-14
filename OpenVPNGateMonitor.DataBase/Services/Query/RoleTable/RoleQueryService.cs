using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.RoleTable;

public class RoleQueryService(
    IQueryService<Role, int> q) : IRoleQueryService
{
    public Task<List<Role>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<Role?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<IPagedResult<Role>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);

    public Task<List<Role>> Search(
        Expression<Func<Role, bool>> predicate,
        CancellationToken ct)
        => q.Where(predicate, ct: ct);
}