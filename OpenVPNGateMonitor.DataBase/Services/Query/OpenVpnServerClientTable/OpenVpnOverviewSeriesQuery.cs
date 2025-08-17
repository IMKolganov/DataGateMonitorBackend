// OpenVPNGateMonitor.DataBase.Services.Query.Overview/OpenVpnOverviewSeriesQuery.cs

using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

/// <summary>
// — Overview series without relying on LastUpdate/end-of-session.
// — Each DB row is treated as a whole session, and ALL session bytes are
// — attributed to the bucket where the session started (ConnectedSince).
// — "ActiveClients" per bucket is the number of sessions that started in that bucket.
// — Buckets are aligned by the offset of 'fromUtc' so days/months/years match local calendar.
// — Always returns a continuous (gap-filled) series.
// </summary>
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
        // Normalize bounds
        if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

        // Define bucket grid using the offset of 'fromUtc' (e.g. +03:00)
        var offset = fromUtc.Offset;

        // Decide grouping (mirrors frontend auto logic)
        var span = toUtc - fromUtc;
        var mode = grouping == OverviewGrouping.Auto
            ? span <= TimeSpan.FromDays(2)       ? OverviewGrouping.Hours
            : span <= TimeSpan.FromDays(180)     ? OverviewGrouping.Days
            : span <= TimeSpan.FromDays(36 * 30) ? OverviewGrouping.Months
            : OverviewGrouping.Years
            : grouping;

        // Filter: sessions that START inside [from; to)
        var q = uow.GetQuery<OpenVpnServerClient>().AsQueryable();

        if (vpnServerId.HasValue)
            q = q.Where(s => s.VpnServerId == vpnServerId.Value);

        if (!string.IsNullOrWhiteSpace(externalId))
            q = q.Where(s => s.ExternalId == externalId!); // exact match; change to EF.Functions.ILike if needed

        // IMPORTANT: compare DateTimeOffset to DateTimeOffset (no .UtcDateTime)
        q = q.Where(s =>
                s.ConnectedSince >= fromUtc &&
                s.ConnectedSince <  toUtc)
             .AsNoTracking();

        // Project minimal fields
        var sessions = await q.Select(s => new SessionStartRow
        {
            VpnServerId       = s.VpnServerId,
            ConnectedSinceUtc = s.ConnectedSince, // already an instant (timestamptz -> DateTimeOffset)
            BytesIn           = s.BytesReceived,
            BytesOut          = s.BytesSent
        }).ToListAsync(ct);

        // Aggregate into buckets (anchor-at-start)
        var buckets = new Dictionary<DateTimeOffset, Accum>(capacity: 512);

        foreach (var s in sessions)
        {
            // enforce offset = 0 for the bucket keys
            var startUtc = s.ConnectedSinceUtc.ToOffset(TimeSpan.Zero);
            var startBucket = AlignToBucketStartWithOffset(mode, startUtc, offset);

            ref var acc = ref GetOrAddRef(buckets, startBucket);
            acc.SessionStarts += 1;                   // count a start in this bucket
            acc.In  += Math.Max(0, s.BytesIn);        // sum bytes
            acc.Out += Math.Max(0, s.BytesOut);
        }

        // Build series
        var series = buckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new OverviewSeriesRow
            {
                Ts                = kv.Key,
                ActiveClients     = kv.Value.SessionStarts,   // number of starts in bucket
                TrafficInBytes    = kv.Value.In,
                TrafficOutBytes   = kv.Value.Out,
                TrafficTotalBytes = kv.Value.In + kv.Value.Out
            })
            .ToList();

        // Zero-fill gaps so X axis is continuous
        series = FillMissingBuckets(series, fromUtc, toUtc, mode, offset);

        return new OverviewSeriesResponse
        {
            Meta = new OverviewMeta
            {
                From        = fromUtc,
                To          = toUtc,
                Grouping    = mode.ToString().ToLowerInvariant(),
                Timezone    = "UTC",   // timestamps remain in UTC; bucket grid uses 'offset'
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

    private sealed class SessionStartRow
    {
        public int VpnServerId { get; set; }
        public DateTimeOffset ConnectedSinceUtc { get; set; } // use DTO for an instant
        public long BytesIn { get; set; }
        public long BytesOut { get; set; }
    }

    private struct Accum
    {
        public int  SessionStarts;
        public long In;
        public long Out;
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
        if (!exists) entry = default;
        return ref entry;
    }
}
