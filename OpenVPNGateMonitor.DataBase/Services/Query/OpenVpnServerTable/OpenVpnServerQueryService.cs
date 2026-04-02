using System.Linq.Expressions;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public class OpenVpnServerQueryService(
    IQueryService<OpenVpnServer, int> q,
    IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService) : IOpenVpnServerQueryService
{
    public async Task<List<OpenVpnServer>> GetAll(bool includeDeleted = false, bool requireQuotaPlanAssignment = false,
        CancellationToken ct = default)
    {
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

    public Task<OpenVpnServer?> GetById(int id, CancellationToken ct = default)
        => q.FindById(id, ct: ct);

    public Task<List<OpenVpnServer>> GetDefaultExcept(int exceptId, CancellationToken ct = default)
        => q.Where(x => x.IsDefault && x.Id != exceptId && !x.IsDeleted, ct: ct);

    public async Task<IPagedResult<OpenVpnServer>> GetPage(int page, int pageSize, bool includeDeleted = false,
        bool requireQuotaPlanAssignment = false, CancellationToken ct = default)
    {
        if (!requireQuotaPlanAssignment)
        {
            return includeDeleted
                ? await q.Page(page, pageSize, ct: ct)
                : await q.Page(page, pageSize, predicate: x => !x.IsDeleted, ct: ct);
        }

        var allowedIds = await quotaPlanAllowedServerQueryService.GetDistinctVpnServerIds(ct);
        if (allowedIds.Count == 0)
        {
            return new PagedResponse<OpenVpnServer>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                Items = []
            };
        }

        Expression<Func<OpenVpnServer, bool>> predicate = includeDeleted
            ? x => allowedIds.Contains(x.Id)
            : x => allowedIds.Contains(x.Id) && !x.IsDeleted;

        return await q.Page(page, pageSize, predicate: predicate, ct: ct);
    }

    public Task<bool> AnyByServerName(string serverName, CancellationToken ct = default)
        => q.Any(x => x.ServerName == serverName && !x.IsDeleted, ct: ct);

    public Task<bool> AnyByServerNameExceptId(string serverName, int id, CancellationToken ct = default)
        => q.Any(x => x.ServerName == serverName && x.Id != id && !x.IsDeleted, ct: ct);
}
