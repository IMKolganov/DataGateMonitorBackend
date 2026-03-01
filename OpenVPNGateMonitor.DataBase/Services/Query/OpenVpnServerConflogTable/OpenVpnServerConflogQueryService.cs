using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerConflogTable;

public class OpenVpnServerConflogQueryService(IQueryService<OpenVpnServerConflog, int> q)
    : IOpenVpnServerConflogQueryService
{
    public Task<OpenVpnServerConflog?> GetById(int id, CancellationToken ct = default)
        => q.FindById(id, ct: ct);

    public Task<OpenVpnServerConflog?> GetLastByVpnServerId(int vpnServerId, CancellationToken ct = default)
        => q.FirstOrDefault(
            predicate: x => x.VpnServerId == vpnServerId,
            orderBy: q => q.OrderByDescending(x => x.CreateDate),
            ct: ct);

    public Task<OpenVpnServerConflog?> GetLastByRequestUrl(string requestUrl, CancellationToken ct = default)
        => q.FirstOrDefault(
            predicate: x => x.RequestUrl == requestUrl,
            orderBy: q => q.OrderByDescending(x => x.CreateDate),
            ct: ct);

    public async Task<IPagedResult<OpenVpnServerConflog>> GetPageByVpnServerId(
        int vpnServerId, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var baseQuery = q.Query()
            .Where(x => x.VpnServerId == vpnServerId);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(x => x.CreateDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResponse<OpenVpnServerConflog>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public Task<IPagedResult<OpenVpnServerConflog>> GetPage(int page, int pageSize, CancellationToken ct = default)
        => q.Page(page, pageSize, orderBy: q => q.OrderByDescending(x => x.CreateDate), ct: ct);
}
