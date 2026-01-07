using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;

public interface IUserRefreshTokenQueryService
{
    Task<List<UserRefreshToken>> GetAll(CancellationToken ct);
    Task<UserRefreshToken?> GetByTokenHash(string tokenHash, CancellationToken ct);
    Task<UserRefreshToken?> GetById(int id, CancellationToken ct);
    Task<IPagedResult<UserRefreshToken>> GetPage(int page, int pageSize, CancellationToken ct);
    public Task<List<UserRefreshToken>> Search(
        Expression<Func<UserRefreshToken, bool>> predicate,
        CancellationToken ct);
}