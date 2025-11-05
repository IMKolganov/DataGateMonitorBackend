using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.UserTable;

public class UserQueryService(
    IQueryService<User, int> q,
    IQueryService<UserIdentityLink, int> qUserIdentityLink
) : IUserQueryService
{
    public Task<List<User>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct)
    {
        var link = await qUserIdentityLink.FirstOrDefaultAsync(
            predicate: x => x.ExternalId == externalId,
            asNoTracking: true,
            ct: ct
        );

        if (link is null)
            return null;

        return await q.FirstOrDefaultAsync(
            predicate: x => x.Id == link.UserId,
            asNoTracking: true,
            ct: ct
        );
    }

    public Task<IPagedResult<User>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}