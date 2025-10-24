using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;

public class UserIdentityLinkQueryService(IQueryService<UserIdentityLink, int> q) : IUserIdentityLinkQueryService
{
    public Task<List<UserIdentityLink>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<UserIdentityLink?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<UserIdentityLink?> GetByProviderAndExternalId(string provider, string externalId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.Provider == provider && x.ExternalId == externalId, ct);

    public Task<UserIdentityLink?> GetByUserId(int userId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public Task<bool> AnyByUserId(int userId, CancellationToken ct)
        => q.AnyAsync(x => x.UserId == userId, ct: ct);

    public Task<IPagedResult<UserIdentityLink>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}