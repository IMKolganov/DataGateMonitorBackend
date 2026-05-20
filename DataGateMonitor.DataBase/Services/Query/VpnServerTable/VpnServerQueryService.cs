using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerTable;

public class VpnServerQueryService(
    IQueryService<VpnServer, int> q,
    IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService) : IVpnServerQueryService
{
    public async Task<List<VpnServer>> GetAll(bool includeDeleted = false, bool requireQuotaPlanAssignment = false,
        int? restrictToQuotaPlanId = null, CancellationToken ct = default)
    {
        if (restrictToQuotaPlanId is int planId)
        {
            var ids = await quotaPlanAllowedServerQueryService.GetVpnServerIdsByQuotaPlanId(planId, ct);
            if (ids.Count == 0)
                return [];

            return includeDeleted
                ? await q.Where(x => ids.Contains(x.Id), ct: ct)
                : await q.Where(x => ids.Contains(x.Id) && !x.IsDeleted, ct: ct);
        }

        if (!requireQuotaPlanAssignment)
        {
            return includeDeleted
                ? await q.GetAll(ct: ct)
                : await q.Where(x => !x.IsDeleted, ct: ct);
        }

        var allowedIds = await quotaPlanAllowedServerQueryService.GetDistinctVpnServerIds(ct);
        if (allowedIds.Count == 0)
            return [];

        if (includeDeleted)
            return await q.Where(x => allowedIds.Contains(x.Id), ct: ct);

        return await q.Where(x => allowedIds.Contains(x.Id) && !x.IsDeleted, ct: ct);
    }

    public Task<VpnServer?> GetById(int id, CancellationToken ct = default)
        => q.FindById(id, ct: ct);

    public Task<List<VpnServer>> GetDefaultExcept(int exceptId, CancellationToken ct = default)
        => q.Where(x => x.IsDefault && x.Id != exceptId && !x.IsDeleted, ct: ct);

    public async Task<IPagedResult<VpnServer>> GetPage(int page, int pageSize, bool includeDeleted = false,
        bool requireQuotaPlanAssignment = false, int? restrictToQuotaPlanId = null, CancellationToken ct = default)
    {
        if (restrictToQuotaPlanId is int planId)
        {
            var ids = await quotaPlanAllowedServerQueryService.GetVpnServerIdsByQuotaPlanId(planId, ct);
            if (ids.Count == 0)
            {
                return new PagedResponse<VpnServer>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    Items = []
                };
            }

            Expression<Func<VpnServer, bool>> predicate = includeDeleted
                ? x => ids.Contains(x.Id)
                : x => ids.Contains(x.Id) && !x.IsDeleted;

            return await q.Page(page, pageSize, predicate: predicate, ct: ct);
        }

        if (!requireQuotaPlanAssignment)
        {
            return includeDeleted
                ? await q.Page(page, pageSize, ct: ct)
                : await q.Page(page, pageSize, predicate: x => !x.IsDeleted, ct: ct);
        }

        var allowedIds = await quotaPlanAllowedServerQueryService.GetDistinctVpnServerIds(ct);
        if (allowedIds.Count == 0)
        {
            return new PagedResponse<VpnServer>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                Items = []
            };
        }

        Expression<Func<VpnServer, bool>> predicate2 = includeDeleted
            ? x => allowedIds.Contains(x.Id)
            : x => allowedIds.Contains(x.Id) && !x.IsDeleted;

        return await q.Page(page, pageSize, predicate: predicate2, ct: ct);
    }

    public Task<bool> AnyByServerName(string serverName, CancellationToken ct = default)
        => q.Any(x => x.ServerName == serverName && !x.IsDeleted, ct: ct);

    public Task<bool> AnyByServerNameExceptId(string serverName, int id, CancellationToken ct = default)
        => q.Any(x => x.ServerName == serverName && x.Id != id && !x.IsDeleted, ct: ct);

    public async Task<DateTimeOffset?> GetLastUpdateStamp(bool includeDeleted = false,
        bool requireQuotaPlanAssignment = false, int? restrictToQuotaPlanId = null, CancellationToken ct = default)
    {
        if (restrictToQuotaPlanId is int planId)
        {
            var ids = await quotaPlanAllowedServerQueryService.GetVpnServerIdsByQuotaPlanId(planId, ct);
            if (ids.Count == 0)
                return null;

            var scoped = q.Query().Where(x => ids.Contains(x.Id));
            if (!includeDeleted)
                scoped = scoped.Where(x => !x.IsDeleted);

            return await scoped.MaxAsync(x => (DateTimeOffset?)x.LastUpdate, ct);
        }

        if (!requireQuotaPlanAssignment)
        {
            var all = q.Query();
            if (!includeDeleted)
                all = all.Where(x => !x.IsDeleted);

            return await all.MaxAsync(x => (DateTimeOffset?)x.LastUpdate, ct);
        }

        var allowedIds = await quotaPlanAllowedServerQueryService.GetDistinctVpnServerIds(ct);
        if (allowedIds.Count == 0)
            return null;

        var filtered = q.Query().Where(x => allowedIds.Contains(x.Id));
        if (!includeDeleted)
            filtered = filtered.Where(x => !x.IsDeleted);

        return await filtered.MaxAsync(x => (DateTimeOffset?)x.LastUpdate, ct);
    }
}
