using System.Linq.Expressions;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.RoleTable;

public interface IRoleQueryService
{
    Task<List<Role>> GetAll(CancellationToken ct);
    Task<Role?> GetById(int id, CancellationToken ct);
    Task<IPagedResult<Role>> GetPage(int page, int pageSize, CancellationToken ct);
    public Task<List<Role>> Search(
        Expression<Func<Role, bool>> predicate,
        CancellationToken ct);
}