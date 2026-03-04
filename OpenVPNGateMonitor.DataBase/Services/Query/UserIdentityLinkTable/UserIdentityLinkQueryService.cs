using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;

public class UserIdentityLinkQueryService(IQueryService<UserIdentityLink, int> q) : IUserIdentityLinkQueryService
{
    public Task<List<UserIdentityLink>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<UserIdentityLink?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<UserIdentityLink?> GetByProviderAndExternalId(string provider, string externalId, 
        CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ExternalId == externalId, ct);
    
    public Task<UserIdentityLink?> GetByExternalId(string externalId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.ExternalId == externalId, ct);

    public Task<UserIdentityLink?> GetByUserId(int userId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public Task<bool> AnyByUserId(int userId, CancellationToken ct)
        => q.Any(x => x.UserId == userId, ct: ct);

    public Task<IPagedResult<UserIdentityLink>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}