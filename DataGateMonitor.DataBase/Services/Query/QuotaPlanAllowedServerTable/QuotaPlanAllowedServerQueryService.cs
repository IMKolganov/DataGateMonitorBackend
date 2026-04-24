using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;

public class QuotaPlanAllowedServerQueryService(
    IQueryService<QuotaPlanAllowedServer, int> q) : IQuotaPlanAllowedServerQueryService
{
    public async Task<HashSet<int>> GetDistinctVpnServerIds(CancellationToken ct)
    {
        var ids = await q.Query()
            .Select(x => x.VpnServerId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<HashSet<int>> GetVpnServerIdsByQuotaPlanId(int quotaPlanId, CancellationToken ct)
    {
        var list = await q.Where(x => x.QuotaPlanId == quotaPlanId, ct: ct);
        return list.Select(x => x.VpnServerId).ToHashSet();
    }

    public Task<List<QuotaPlanAllowedServer>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<QuotaPlanAllowedServer?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<QuotaPlanAllowedServer?> GetByQuotaPlanIdAndServerId(int quotaPlanId, int vpnServerId,
        CancellationToken ct)
        => q.FirstOrDefault(
            predicate: x => x.QuotaPlanId == quotaPlanId && x.VpnServerId == vpnServerId,
            orderBy: s => s.OrderBy(x => x.Id),
            asNoTracking: true,
            ct: ct);

    public Task<List<QuotaPlanAllowedServer>> GetListByQuotaPlanId(int quotaPlanId, CancellationToken ct)
        => q.Where(x => x.QuotaPlanId == quotaPlanId, ct: ct);

    public Task<List<QuotaPlanAllowedServer>> GetListByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.Where(x => x.VpnServerId == vpnServerId, ct: ct);

    public Task<IPagedResult<QuotaPlanAllowedServer>> GetPage(int page, int pageSize, int? quotaPlanId, int? vpnServerId, CancellationToken ct)
    {
        System.Linq.Expressions.Expression<Func<QuotaPlanAllowedServer, bool>>? predicate = null;
        if (quotaPlanId is > 0 && vpnServerId is > 0)
            predicate = x => x.QuotaPlanId == quotaPlanId && x.VpnServerId == vpnServerId;
        else if (quotaPlanId is > 0)
            predicate = x => x.QuotaPlanId == quotaPlanId;
        else if (vpnServerId is > 0)
            predicate = x => x.VpnServerId == vpnServerId;

        return q.Page(page, pageSize, predicate: predicate, orderBy: s => s.OrderBy(x => x.Id), ct: ct);
    }
}