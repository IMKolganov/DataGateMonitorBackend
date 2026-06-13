using System.Linq.Expressions;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;

public sealed class UserRefreshTokenQueryService(
    IQueryService<UserRefreshToken, int> q) : IUserRefreshTokenQueryService
{
    public Task<List<UserRefreshToken>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<UserRefreshToken?> GetByTokenHash(string tokenHash, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        return q.FirstOrDefault(x => x.TokenHash == tokenHash, ct: ct);
    }

    public Task<UserRefreshToken?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<IPagedResult<UserRefreshToken>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);

    public Task<List<UserRefreshToken>> Search(
        Expression<Func<UserRefreshToken, bool>> predicate,
        CancellationToken ct)
        => q.Where(predicate, ct: ct);
}