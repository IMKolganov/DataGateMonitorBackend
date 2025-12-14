using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;

public class UserCredentialQueryService(IQueryService<UserCredential, int> q)
    : IUserCredentialQueryService
{
    public Task<List<UserCredential>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<UserCredential?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<UserCredential?> GetByNormalizedLogin(string normalizedLogin, CancellationToken ct)
        => q.FirstOrDefault(x => x.NormalizedLogin == normalizedLogin, asNoTracking: true, ct: ct);

    public Task<UserCredential?> GetByLogin(string login, CancellationToken ct)
    {
        var normalized = login.ToUpperInvariant();
        return q.FirstOrDefault(x => x.NormalizedLogin == normalized, asNoTracking: true, ct: ct);
    }

    public Task<UserCredential?> GetByUserId(int userId, CancellationToken ct)
        => q.FirstOrDefault(x => x.UserId == userId, asNoTracking: true, ct: ct);

    public Task<bool> AnyByUserId(int userId, CancellationToken ct)
        => q.Any(x => x.UserId == userId, ct: ct);

    public Task<bool> LoginExists(string login, CancellationToken ct)
    {
        var normalized = login.ToUpperInvariant();
        return q.Any(x => x.NormalizedLogin == normalized, ct: ct);
    }

    public Task<IPagedResult<UserCredential>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}