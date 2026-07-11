using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerConflogTable;

public class VpnServerConflogQueryService(IQueryService<VpnServerConflog, int> q)
    : IVpnServerConflogQueryService
{
    public Task<VpnServerConflog?> GetById(int id, CancellationToken ct = default)
        => q.FindById(id, ct: ct);

    public Task<VpnServerConflog?> GetLastByVpnServerId(int vpnServerId, CancellationToken ct = default)
        => q.FirstOrDefault(
            predicate: x => x.VpnServerId == vpnServerId,
            orderBy: q => q.OrderByDescending(x => x.CreateDate),
            ct: ct);

    public Task<VpnServerConflog?> GetLastByRequestUrl(string requestUrl, CancellationToken ct = default)
        => q.FirstOrDefault(
            predicate: x => x.RequestUrl == requestUrl,
            orderBy: q => q.OrderByDescending(x => x.CreateDate),
            ct: ct);

    public async Task<IPagedResult<VpnServerConflog>> GetPageByVpnServerId(
        int vpnServerId, int page, int pageSize, CancellationToken ct = default)
        => await GetPageByVpnServerId(vpnServerId, page, pageSize, requestUrl: null, ct);

    public async Task<IPagedResult<VpnServerConflog>> GetPageByVpnServerId(
        int vpnServerId, int page, int pageSize, string? requestUrl, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var baseQuery = q.Query()
            .Where(x => x.VpnServerId == vpnServerId);

        var urlPattern = GridFilterHelper.ContainsPattern(requestUrl);
        if (urlPattern != null)
            baseQuery = baseQuery.Where(x => x.RequestUrl != null && EF.Functions.ILike(x.RequestUrl, urlPattern));

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(x => x.CreateDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResponse<VpnServerConflog>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public Task<IPagedResult<VpnServerConflog>> GetPage(int page, int pageSize, CancellationToken ct = default)
        => q.Page(page, pageSize, orderBy: q => q.OrderByDescending(x => x.CreateDate), ct: ct);
}
