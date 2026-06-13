using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

/// <summary>
/// Overview totals over sessions and traffic (traffic aggregated in PostgreSQL or in-memory for tests).
/// </summary>
public sealed class OpenVpnOverviewTotalsQuery(
    IUnitOfWork uow,
    IOverviewTrafficAggregator trafficAggregator) : IOpenVpnOverviewTotalsQuery
{
    public async Task<OverviewTotalsResponse> GetOverviewTotalsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default)
    {
        if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

        var sessionsQ = uow.GetQuery<VpnServerClient>().AsQueryable();

        if (vpnServerId.HasValue)
            sessionsQ = sessionsQ.Where(x => x.VpnServerId == vpnServerId.Value);

        if (!string.IsNullOrWhiteSpace(externalId))
            sessionsQ = sessionsQ.Where(x => x.ExternalId == externalId!);

        sessionsQ = sessionsQ
            .Where(x => x.ConnectedSince >= fromUtc && x.ConnectedSince < toUtc)
            .AsNoTracking();

        var sessionsCount = await sessionsQ.LongCountAsync(ct);

        var usersCount = await sessionsQ
            .Select(x => x.ExternalId)
            .Where(x => x != null && x != "")
            .Distinct()
            .LongCountAsync(ct);

        var trafficTotals = await trafficAggregator.GetTrafficTotalsAsync(
            fromUtc, toUtc, vpnServerId, externalId, ct);

        return new OverviewTotalsResponse
        {
            Meta = new OverviewMetaDto
            {
                From = fromUtc,
                To = toUtc,
                Grouping = "none",
                Timezone = "UTC",
                TrafficUnit = "bytes",
                VpnServerId = vpnServerId
            },
            Totals = new TotalsPayloadDto
            {
                SessionsCount = sessionsCount,
                UsersCount = usersCount,
                TrafficInBytes = trafficTotals.TrafficInBytes,
                TrafficOutBytes = trafficTotals.TrafficOutBytes
            }
        };
    }
}
