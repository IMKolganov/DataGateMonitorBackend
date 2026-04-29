using System.Linq.Expressions;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.UserTable;

public class UserQueryService(
    IQueryService<User, int> q,
    IQueryService<UserIdentityLink, int> qUserIdentityLink
) : IUserQueryService
{
    public Task<List<User>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<User?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public async Task<User?> GetByEmail(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToUpperInvariant();
        return await q.FirstOrDefault(
            predicate: u => u.Email != null && u.Email.ToUpper() == normalizedEmail,
            asNoTracking: true,
            ct: ct
        );
    }

    public Task<bool> AnyByEmail(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult(false);

        var normalizedEmail = email.Trim().ToUpperInvariant();
        return q.Any(x => x.Email != null && x.Email.ToUpper() == normalizedEmail, ct: ct);
    }

    public async Task<User?> GetByExternalId(string externalId, CancellationToken ct)
    {
        var link = await qUserIdentityLink.FirstOrDefault(
            predicate: x => x.ExternalId == externalId,
            asNoTracking: true,
            ct: ct
        );

        if (link is null)
            return null;

        return await q.FirstOrDefault(
            predicate: x => x.Id == link.UserId,
            asNoTracking: true,
            ct: ct
        );
    }

    public Task<IPagedResult<User>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(
            page: page, 
            pageSize: pageSize, 
            orderBy: x => x.OrderByDescending(u => u.Id),
            ct: ct);

    public Task<List<User>> Search(
        Expression<Func<User, bool>> predicate,
        CancellationToken ct)
        => q.Where(predicate, ct: ct);

    public Task<List<User>> GetUsersWithNonEmptyEmailAsync(CancellationToken ct) =>
        q.Where(
            predicate: u => u.Email != null && u.Email != "",
            orderBy: x => x.OrderBy(u => u.Id),
            asNoTracking: true,
            ct: ct);
}