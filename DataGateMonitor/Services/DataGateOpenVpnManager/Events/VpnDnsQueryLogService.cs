using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.PiHole.Requests;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Events;

public class VpnDnsQueryLogService(
    ICommandService<VpnDnsQueryLog, int> cmd,
    IQueryService<VpnDnsQueryLog, int> query,
    IIssuedOvpnFileQueryService issuedOvpnFileQueryService) : IVpnDnsQueryLogService
{
    public async Task<int> SaveBatchAsync(int vpnServerId, DnsQueryBatchRequest batch, CancellationToken ct)
    {
        if (batch.Queries.Count == 0)
            return 0;

        var piHoleIds = batch.Queries.Select(q => q.PiHoleQueryId).Distinct().ToList();
        var existingIds = await query.Query()
            .Where(x => x.VpnServerId == vpnServerId && piHoleIds.Contains(x.PiHoleQueryId))
            .Select(x => x.PiHoleQueryId)
            .ToListAsync(ct);
        var existingSet = existingIds.ToHashSet();

        var now = DateTimeOffset.UtcNow;
        var rows = new List<VpnDnsQueryLog>();

        foreach (var query in batch.Queries)
        {
            if (existingSet.Contains(query.PiHoleQueryId))
                continue;

            string? externalId = null;
            if (!string.IsNullOrWhiteSpace(query.CommonName))
            {
                externalId = await issuedOvpnFileQueryService.GetExternalIdByCommonName(
                    query.CommonName, vpnServerId, ct);
            }

            rows.Add(new VpnDnsQueryLog
            {
                VpnServerId = vpnServerId,
                PiHoleQueryId = query.PiHoleQueryId,
                CommonName = query.CommonName,
                ExternalId = externalId,
                ClientIp = query.ClientIp,
                Domain = query.Domain,
                QueryType = query.QueryType,
                Status = query.Status,
                QueriedAtUtc = query.QueriedAtUtc,
                CreateDate = now,
                LastUpdate = now
            });
        }

        if (rows.Count == 0)
            return 0;

        await cmd.AddRange(rows, saveChanges: true, ct);
        return rows.Count;
    }
}
