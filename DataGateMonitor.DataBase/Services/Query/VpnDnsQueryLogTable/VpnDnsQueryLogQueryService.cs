using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.DataBase.Services.Query.VpnDnsQueryLogTable;

public class VpnDnsQueryLogQueryService(IQueryService<VpnDnsQueryLog, int> q) : IVpnDnsQueryLogQueryService
{
    public async Task<IPagedResult<VpnDnsQueryLog>> SearchAsync(GetVpnDnsQueryRequest request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 50 : Math.Min(request.PageSize, 500);

        var query = q.Query();

        if (request.VpnServerId > 0)
            query = query.Where(x => x.VpnServerId == request.VpnServerId);

        if (!string.IsNullOrWhiteSpace(request.ExternalId))
            query = query.Where(x => x.ExternalId == request.ExternalId);

        if (!string.IsNullOrWhiteSpace(request.CommonName))
            query = query.Where(x => x.CommonName == request.CommonName);

        if (!string.IsNullOrWhiteSpace(request.ClientIp))
            query = query.Where(x => x.ClientIp == request.ClientIp);

        if (!string.IsNullOrWhiteSpace(request.DomainContains))
        {
            var needle = request.DomainContains.Trim().ToLowerInvariant();
            query = query.Where(x => x.Domain.ToLower().Contains(needle));
        }

        if (request.FromUtc.HasValue)
            query = query.Where(x => x.QueriedAtUtc >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(x => x.QueriedAtUtc <= request.ToUtc.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.QueriedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResponse<VpnDnsQueryLog>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<(int TotalCount, DateTimeOffset? LastQueriedAtUtc)> GetServerSummaryAsync(
        int vpnServerId,
        CancellationToken ct)
    {
        if (vpnServerId <= 0)
            return (0, null);

        var query = q.Query().Where(x => x.VpnServerId == vpnServerId);
        var totalCount = await query.CountAsync(ct);
        if (totalCount == 0)
            return (0, null);

        var lastAt = await query.MaxAsync(x => x.QueriedAtUtc, ct);
        return (totalCount, lastAt);
    }
}
