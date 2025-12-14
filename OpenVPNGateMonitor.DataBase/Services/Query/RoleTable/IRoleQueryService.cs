using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.RoleTable;

public interface IRoleQueryService
{
    Task<List<Role>> GetAll(CancellationToken ct);
    Task<Role?> GetById(int id, CancellationToken ct);
    Task<IPagedResult<Role>> GetPage(int page, int pageSize, CancellationToken ct);
    public Task<List<Role>> Search(
        Expression<Func<Role, bool>> predicate,
        CancellationToken ct);
}