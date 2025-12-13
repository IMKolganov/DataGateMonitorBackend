using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.RoleTable;

public interface IRoleQueryService
{
    Task<List<Role>> GetAllAsync(CancellationToken ct);
    Task<Role?> GetByIdAsync(int id, CancellationToken ct);
    Task<IPagedResult<Role>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    public Task<List<Role>> SearchAsync(
        Expression<Func<Role, bool>> predicate,
        CancellationToken ct);
}