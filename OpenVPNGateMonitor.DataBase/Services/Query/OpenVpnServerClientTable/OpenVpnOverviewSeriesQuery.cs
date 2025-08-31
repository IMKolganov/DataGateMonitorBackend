using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

/// <summary>
/// Overview series over traffic samples:
/// - Uses OpenVpnServerClientTraffic (cumulative counters).
/// - Per session, computes deltas between consecutive samples inside [from;to).
/// - Aggregates deltas into time buckets (Hours/Days/Months/Years) using the offset of 'fromUtc'.
/// - ActiveClients per bucket = number of distinct sessions that had at least one sample in that bucket.
/// - Always returns a continuous (gap-filled) series.
/// </summary>
public sealed class OpenVpnOverviewSeriesQuery(IUnitOfWork uow) : IOpenVpnOverviewSeriesQuery
{
    // Backward-compatible signature (no externalId)
    public Task<OverviewSeriesResponse> GetOverviewSeriesFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        CancellationToken ct = default)
        => GetOverviewSeriesFromSessionsAsync(fromUtc, toUtc, grouping, vpnServerId, externalId: null, ct);

    // New signature with optional externalId filter
    public async Task<OverviewSeriesResponse> GetOverviewSeriesFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default)
    {
        if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

        var offset = fromUtc.Offset;
        var span = toUtc - fromUtc;
        var mode = grouping == OverviewGrouping.Auto
            ? span <= TimeSpan.FromDays(2)       ? OverviewGrouping.Hours
            : span <= TimeSpan.FromDays(180)     ? OverviewGrouping.Days
            : span <= TimeSpan.FromDays(36 * 30) ? OverviewGrouping.Months
            : OverviewGrouping.Years
            : grouping;

        // ---- Query traffic samples inside window ----
        var q = uow.GetQuery<OpenVpnServerClientTraffic>().AsQueryable();

        if (vpnServerId.HasValue)
            q = q.Where(s => s.VpnServerId == vpnServerId.Value);

        if (!string.IsNullOrWhiteSpace(externalId))
            q = q.Where(s => s.ExternalId == externalId!);

        // Important: compare DateTimeOffset to DateTimeOffset (keep tz info)
        q = q.Where(s => s.MeasuredAt >= fromUtc && s.MeasuredAt < toUtc)
             .AsNoTracking();

        var samples = await q
            .Select(s => new TrafficSampleRow
            {
                VpnServerId = s.VpnServerId,
                SessionId   = s.SessionId,
                MeasuredAt  = s.MeasuredAt,                 // timestamptz -> DateTimeOffset
                BytesIn     = s.BytesReceived,
                BytesOut    = s.BytesSent
            })
            .OrderBy(s => s.SessionId)
            .ThenBy(s => s.MeasuredAt)
            .ToListAsync(ct);

        // ---- Aggregate deltas into buckets ----
        var buckets = new Dictionary<DateTimeOffset, Accum>(capacity: 1024);

        // Keep previous sample per session to compute deltas
        var lastBySession = new Dictionary<Guid, (DateTimeOffset ts, long inTot, long outTot)>(capacity: 1024);

        foreach (var s in samples)
        {
            var tsUtc = s.MeasuredAt.ToOffset(TimeSpan.Zero);
            var bucket = AlignToBucketStartWithOffset(mode, tsUtc, offset);

            ref var acc = ref GetOrAddRef(buckets, bucket);
            acc.SeenSessions ??= new HashSet<Guid>();
            acc.SeenSessions.Add(s.SessionId);

            if (lastBySession.TryGetValue(s.SessionId, out var prev))
            {
                // cumulative -> delta (protect against resets)
                var dIn  = s.BytesIn  >= prev.inTot  ? (s.BytesIn  - prev.inTot)  : s.BytesIn;
                var dOut = s.BytesOut >= prev.outTot ? (s.BytesOut - prev.outTot) : s.BytesOut;

                if (dIn  < 0) dIn  = 0;
                if (dOut < 0) dOut = 0;

                acc.In  += dIn;
                acc.Out += dOut;
            }

            // move window
            lastBySession[s.SessionId] = (s.MeasuredAt, s.BytesIn, s.BytesOut);
        }

        // ---- Build series rows ----
        var series = buckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new OverviewSeriesRow
            {
                Ts                = kv.Key,
                ActiveClients     = kv.Value.SeenSessions?.Count ?? 0,
                TrafficInBytes    = kv.Value.In,
                TrafficOutBytes   = kv.Value.Out,
                TrafficTotalBytes = kv.Value.In + kv.Value.Out
            })
            .ToList();

        // Zero-fill to continuous X-axis
        series = FillMissingBuckets(series, fromUtc, toUtc, mode, offset);

        return new OverviewSeriesResponse
        {
            Meta = new OverviewMeta
            {
                From        = fromUtc,
                To          = toUtc,
                Grouping    = mode.ToString().ToLowerInvariant(),
                Timezone    = "UTC",   // timestamps are UTC; grid is aligned by 'offset'
                TrafficUnit = "bytes",
                VpnServerId = vpnServerId
            },
            Summary = new OverviewSummary
            {
                TotalTrafficInBytes  = series.Sum(r => r.TrafficInBytes),
                TotalTrafficOutBytes = series.Sum(r => r.TrafficOutBytes),
                PeakActiveClients    = series.Count == 0 ? 0 : series.Max(r => r.ActiveClients)
            },
            Series = series
        };
    }

    /* ---------- internal helpers/types ---------- */

    private sealed class TrafficSampleRow
    {
        public int VpnServerId { get; set; }
        public Guid SessionId { get; set; }
        public DateTimeOffset MeasuredAt { get; set; }
        public long BytesIn { get; set; }
        public long BytesOut { get; set; }
    }

    private sealed class Accum
    {
        public long In;
        public long Out;
        public HashSet<Guid>? SeenSessions;
    }

    // Offset-aware helpers: align/advance using the chosen offset (bucket grid in local time)
    private static DateTimeOffset Shift(DateTimeOffset t, TimeSpan offset) => t.ToOffset(offset);
    private static DateTimeOffset Unshift(DateTimeOffset t) => t.ToOffset(TimeSpan.Zero);

    private static DateTimeOffset AlignToBucketStartWithOffset(OverviewGrouping g, DateTimeOffset tUtc, TimeSpan offset)
    {
        var t = Shift(tUtc, offset);
        var aligned = g switch
        {
            OverviewGrouping.Hours  => new DateTimeOffset(t.Year, t.Month, t.Day, t.Hour, 0, 0, t.Offset),
            OverviewGrouping.Days   => new DateTimeOffset(t.Year, t.Month, t.Day, 0, 0, 0, t.Offset),
            OverviewGrouping.Months => new DateTimeOffset(t.Year, t.Month, 1, 0, 0, 0, t.Offset),
            OverviewGrouping.Years  => new DateTimeOffset(t.Year, 1, 1, 0, 0, 0, t.Offset),
            _ => new DateTimeOffset(t.Year, t.Month, t.Day, 0, 0, 0, t.Offset)
        };
        return Unshift(aligned);
    }

    private static DateTimeOffset NextBucketWithOffset(OverviewGrouping g, DateTimeOffset bucketUtc, TimeSpan offset)
    {
        var t = Shift(bucketUtc, offset);
        t = g switch
        {
            OverviewGrouping.Hours  => t.AddHours(1),
            OverviewGrouping.Days   => t.AddDays(1),
            OverviewGrouping.Months => t.AddMonths(1),
            OverviewGrouping.Years  => t.AddYears(1),
            _ => t.AddDays(1)
        };
        return Unshift(t);
    }

    private static List<OverviewSeriesRow> FillMissingBuckets(
        List<OverviewSeriesRow> rows,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping mode,
        TimeSpan offset)
    {
        var dict = rows.ToDictionary(r => r.Ts);
        var list = new List<OverviewSeriesRow>();

        if (mode == OverviewGrouping.Months)
        {
            var cur = AlignToBucketStartWithOffset(OverviewGrouping.Months, fromUtc, offset);
            var end = AlignToBucketStartWithOffset(OverviewGrouping.Months, toUtc, offset);
            while (cur <= end)
            {
                if (!dict.TryGetValue(cur, out var r)) r = new OverviewSeriesRow { Ts = cur };
                list.Add(r);
                cur = NextBucketWithOffset(OverviewGrouping.Months, cur, offset);
            }
            return list;
        }

        if (mode == OverviewGrouping.Years)
        {
            var cur = AlignToBucketStartWithOffset(OverviewGrouping.Years, fromUtc, offset);
            var end = AlignToBucketStartWithOffset(OverviewGrouping.Years, toUtc, offset);
            while (cur <= end)
            {
                if (!dict.TryGetValue(cur, out var r)) r = new OverviewSeriesRow { Ts = cur };
                list.Add(r);
                cur = NextBucketWithOffset(OverviewGrouping.Years, cur, offset);
            }
            return list;
        }

        // Hours / Days
        var curDay = AlignToBucketStartWithOffset(mode, fromUtc, offset);
        var endDay = AlignToBucketStartWithOffset(mode, toUtc, offset);
        while (curDay <= endDay)
        {
            if (!dict.TryGetValue(curDay, out var r)) r = new OverviewSeriesRow { Ts = curDay };
            list.Add(r);
            curDay = NextBucketWithOffset(mode, curDay, offset);
        }
        return list;
    }

    // Fast ref access for Dictionary on modern .NET
    private static ref Accum GetOrAddRef(Dictionary<DateTimeOffset, Accum> dict, DateTimeOffset key)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);
        if (!exists) entry = new Accum();
        return ref entry!;
    }
}