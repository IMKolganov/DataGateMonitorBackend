using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;

public class UserRefreshTokenQueryService(
    IQueryService<UserRefreshToken, int> q) : IUserRefreshTokenQueryService
{
    public Task<List<UserRefreshToken>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<UserRefreshToken?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);
    
    public Task<IPagedResult<UserRefreshToken>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);

    public Task<List<UserRefreshToken>> Search(
        Expression<Func<UserRefreshToken, bool>> predicate,
        CancellationToken ct)
        => q.Where(predicate, ct: ct);
}