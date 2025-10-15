using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

/// <summary>
/// Overview totals over sessions and traffic:
/// - SessionsCount = number of records in Sessions table.
/// - UsersCount = number of distinct ExternalIds.
/// - TrafficIn/Out computed as deltas from cumulative counters in traffic table.
/// - Always aggregated across the full [from;to) window.
/// </summary>
public sealed class OpenVpnOverviewTotalsQuery(IUnitOfWork uow) : IOpenVpnOverviewTotalsQuery
{
    public async Task<OverviewTotalsResponse> GetOverviewTotalsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default)
    {
        if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

        // ---- Sessions ----
        var sessionsQ = uow.GetQuery<OpenVpnServerClient>().AsQueryable();

        if (vpnServerId.HasValue)
            sessionsQ = sessionsQ.Where(x => x.VpnServerId == vpnServerId.Value);

        if (!string.IsNullOrWhiteSpace(externalId))
            sessionsQ = sessionsQ.Where(x => x.ExternalId == externalId!);

        sessionsQ = sessionsQ
            .Where(x => x.ConnectedSince >= fromUtc && x.ConnectedSince < toUtc)
            .AsNoTracking();

        var sessionsCount = await sessionsQ.LongCountAsync(ct);

        // distinct users
        var usersCount = await sessionsQ
            .Select(x => x.ExternalId)
            .Where(x => x != null && x != "")
            .Distinct()
            .LongCountAsync(ct);

        // ---- Traffic ----
        var trafficQ = uow.GetQuery<OpenVpnServerClientTraffic>().AsQueryable();

        if (vpnServerId.HasValue)
            trafficQ = trafficQ.Where(s => s.VpnServerId == vpnServerId.Value);

        if (!string.IsNullOrWhiteSpace(externalId))
            trafficQ = trafficQ.Where(s => s.ExternalId == externalId!);

        trafficQ = trafficQ
            .Where(s => s.MeasuredAt >= fromUtc && s.MeasuredAt < toUtc)
            .AsNoTracking();

        var samples = await trafficQ
            .Select(s => new TrafficSampleRow
            {
                SessionId  = s.SessionId,
                MeasuredAt = s.MeasuredAt,
                BytesIn    = s.BytesReceived,
                BytesOut   = s.BytesSent
            })
            .OrderBy(s => s.SessionId)
            .ThenBy(s => s.MeasuredAt)
            .ToListAsync(ct);

        long totalIn = 0;
        long totalOut = 0;
        var lastBySession = new Dictionary<Guid, (long inTot, long outTot)>();

        foreach (var s in samples)
        {
            if (lastBySession.TryGetValue(s.SessionId, out var prev))
            {
                var dIn  = s.BytesIn  >= prev.inTot  ? (s.BytesIn  - prev.inTot)  : s.BytesIn;
                var dOut = s.BytesOut >= prev.outTot ? (s.BytesOut - prev.outTot) : s.BytesOut;

                if (dIn  < 0) dIn  = 0;
                if (dOut < 0) dOut = 0;

                totalIn  += dIn;
                totalOut += dOut;
            }

            lastBySession[s.SessionId] = (s.BytesIn, s.BytesOut);
        }

        return new OverviewTotalsResponse
        {
            Meta = new OverviewMeta
            {
                From        = fromUtc,
                To          = toUtc,
                Grouping    = "none",
                Timezone    = "UTC",
                TrafficUnit = "bytes",
                VpnServerId = vpnServerId
            },
            Totals = new TotalsPayload
            {
                SessionsCount   = sessionsCount,
                UsersCount      = usersCount,
                TrafficInBytes  = totalIn,
                TrafficOutBytes = totalOut
            }
        };
    }
}