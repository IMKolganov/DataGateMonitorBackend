using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerTable;

public sealed class VpnServerQuotaPlanGroupsQuery(IUnitOfWork uow) : IVpnServerQuotaPlanGroupsQuery
{
    public async Task<Dictionary<int, List<QuotaPlanGroupDto>>> GetGroupsByVpnServerIdsAsync(
        IReadOnlyCollection<int> vpnServerIds,
        CancellationToken ct)
    {
        if (vpnServerIds.Count == 0)
            return new Dictionary<int, List<QuotaPlanGroupDto>>();

        var links = uow.GetQuery<QuotaPlanAllowedServer>().AsQueryable();
        var plans = uow.GetQuery<QuotaPlan>().AsQueryable();

        var rows = await (
                from link in links
                join plan in plans on link.QuotaPlanId equals plan.Id
                where vpnServerIds.Contains(link.VpnServerId) && plan.IsActive
                select new { link.VpnServerId, plan.Id, plan.Name })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.VpnServerId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new QuotaPlanGroupDto { Id = x.Id, Name = x.Name }).ToList());
    }
}
