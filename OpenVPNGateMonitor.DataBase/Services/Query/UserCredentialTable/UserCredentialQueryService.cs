using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;

public class UserCredentialQueryService(IQueryService<UserCredential, int> q)
    : IUserCredentialQueryService
{
    public Task<List<UserCredential>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<UserCredential?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<UserCredential?> GetByNormalizedLogin(string normalizedLogin, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.NormalizedLogin == normalizedLogin, ct);

    public Task<UserCredential?> GetByLoginAsync(string login, CancellationToken ct)
    {
        var normalized = login.ToUpperInvariant();
        return q.Query()
            .FirstOrDefaultAsync(x => x.NormalizedLogin == normalized, ct);
    }

    public Task<UserCredential?> GetByUserId(int userId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public Task<bool> AnyByUserId(int userId, CancellationToken ct)
        => q.AnyAsync(x => x.UserId == userId, ct: ct);

    public Task<bool> LoginExistsAsync(string login, CancellationToken ct)
    {
        var normalized = login.ToUpperInvariant();
        return q.AnyAsync(x => x.NormalizedLogin == normalized, ct: ct);
    }

    public Task<IPagedResult<UserCredential>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}