using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerEventLogTable;

public class VpnServerEventLogQueryService(IQueryService<VpnServerEventLog, int> q)
    : IVpnServerEventLogQueryService
{
    public Task<List<VpnServerEventLog>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<VpnServerEventLog?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public async Task<IPagedResult<VpnServerEventLog>> GetByVpnServerId(
        int vpnServerId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var baseQuery = q.Query()
            .Where(x => x.VpnServerId == vpnServerId);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResponse<VpnServerEventLog>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<IPagedResult<VpnServerEventLog>> GetPage(int page, int pageSize, CancellationToken ct)
        => await q.Page(page, pageSize, ct: ct);
}