using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;

public class UserRoleQueryService(
    IQueryService<UserRole, int> q) : IUserRoleQueryService
{
    public Task<List<UserRole>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<UserRole?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<UserRole?> GetByUserId(int userId, CancellationToken ct)
        => q.FirstOrDefault(x=> x.UserId == userId, ct: ct);

    public Task<UserRole?> GetByIdAndUserId(int id, int userId, CancellationToken ct)
        => q.FirstOrDefault(x=> x.Id == id && x.UserId == userId, ct: ct);

    public Task<IPagedResult<UserRole>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);

    public Task<List<UserRole>> Search(
        Expression<Func<UserRole, bool>> predicate,
        CancellationToken ct)
        => q.Where(predicate, ct: ct);
}