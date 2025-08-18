using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;

public class OpenVpnServerEventLogQueryService(IQueryService<OpenVpnServerEventLog, int> q)
    : IOpenVpnServerEventLogQueryService
{
    public Task<List<OpenVpnServerEventLog>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<OpenVpnServerEventLog?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public async Task<IPagedResult<OpenVpnServerEventLog>> GetByVpnServerIdAsync(
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

        return new PagedResponse<OpenVpnServerEventLog>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<IPagedResult<OpenVpnServerEventLog>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => await q.PageAsync(page, pageSize, ct: ct);
}