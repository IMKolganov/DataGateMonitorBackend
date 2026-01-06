using System.Linq.Expressions;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;

public interface IUserRefreshTokenQueryService
{
    Task<List<Models.UserRefreshToken>> GetAll(CancellationToken ct);
    Task<Models.UserRefreshToken?> GetById(int id, CancellationToken ct);
    Task<IPagedResult<Models.UserRefreshToken>> GetPage(int page, int pageSize, CancellationToken ct);
    public Task<List<Models.UserRefreshToken>> Search(
        Expression<Func<Models.UserRefreshToken, bool>> predicate,
        CancellationToken ct);
}