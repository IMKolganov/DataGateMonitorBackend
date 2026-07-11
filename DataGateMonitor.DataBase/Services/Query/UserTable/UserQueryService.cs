using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.UserTable;

public class UserQueryService(
    IQueryService<User, int> q,
    IQueryService<UserIdentityLink, int> qUserIdentityLink,
    IUnitOfWork uow
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
        => GetPage(new GetAllUsersRequest { Page = page, PageSize = pageSize }, ct);

    public async Task<IPagedResult<User>> GetPage(GetAllUsersRequest request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        if (pageSize > 500)
            pageSize = 500;

        var query = uow.GetQuery<User>().AsQueryable();

        var searchPattern = GridFilterHelper.ContainsPattern(request.Search);
        if (searchPattern != null)
        {
            query = query.Where(u =>
                EF.Functions.ILike(u.DisplayName, searchPattern) ||
                (u.Email != null && EF.Functions.ILike(u.Email, searchPattern)));
        }

        var externalIdPattern = GridFilterHelper.ContainsPattern(request.ExternalId);
        var providerPattern = GridFilterHelper.ContainsPattern(request.Provider);
        if (externalIdPattern != null || providerPattern != null)
        {
            var links = uow.GetQuery<UserIdentityLink>().AsQueryable();
            if (externalIdPattern != null)
                links = links.Where(l => EF.Functions.ILike(l.ExternalId, externalIdPattern));
            if (providerPattern != null)
                links = links.Where(l => EF.Functions.ILike(l.Provider, providerPattern));

            var userIds = links.Select(l => l.UserId);
            query = query.Where(u => userIds.Contains(u.Id));
        }

        if (request.IsAdmin.HasValue)
            query = query.Where(u => u.IsAdmin == request.IsAdmin.Value);

        if (request.IsBlocked.HasValue)
            query = query.Where(u => u.IsBlocked == request.IsBlocked.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResponse<User>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

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