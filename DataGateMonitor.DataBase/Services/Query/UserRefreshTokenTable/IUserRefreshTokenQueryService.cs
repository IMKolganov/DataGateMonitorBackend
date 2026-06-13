using System.Linq.Expressions;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;

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