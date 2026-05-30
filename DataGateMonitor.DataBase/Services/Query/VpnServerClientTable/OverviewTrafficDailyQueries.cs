using Microsoft.EntityFrameworkCore;
using Npgsql;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public static class OverviewTrafficDailyQueries
{
    internal readonly record struct RangeSplit(
        DateOnly DailyFrom,
        DateOnly DailyToExclusive,
        DateTimeOffset? RawFromUtc,
        DateTimeOffset? RawToUtc);

    internal static RangeSplit SplitRange(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        if (toUtc < fromUtc)
            (fromUtc, toUtc) = (toUtc, fromUtc);

        var todayStart = StartOfUtcDay(DateTimeOffset.UtcNow);
        var dailyFrom = DateOnly.FromDateTime(fromUtc.UtcDateTime);
        var dailyToExclusive = toUtc <= todayStart
            ? DateOnly.FromDateTime(toUtc.UtcDateTime)
            : DateOnly.FromDateTime(todayStart.UtcDateTime);

        if (fromUtc >= todayStart)
            dailyToExclusive = dailyFrom;

        DateTimeOffset? rawFrom = null;
        DateTimeOffset? rawTo = null;
        if (toUtc > todayStart)
        {
            rawFrom = fromUtc > todayStart ? fromUtc : todayStart;
            rawTo = toUtc;
        }

        return new RangeSplit(dailyFrom, dailyToExclusive, rawFrom, rawTo);
    }

    internal static bool SupportsDailyGrouping(OverviewGrouping grouping)
        => grouping is not OverviewGrouping.Hours;

    internal static async Task<bool> HasDailyRowsAsync(
        ApplicationDbContext ctx,
        DateOnly fromDay,
        DateOnly toDayExclusive,
        CancellationToken ct)
    {
        if (toDayExclusive <= fromDay)
            return false;

        return await ctx.VpnServerClientTrafficDailies.AsNoTracking()
            .AnyAsync(d => d.DayUtc >= fromDay && d.DayUtc < toDayExclusive, ct);
    }

    internal static async Task<IReadOnlyList<OverviewTrafficBucketRow>> QuerySeriesAsync(
        ApplicationDbContext ctx,
        DateOnly fromDay,
        DateOnly toDayExclusive,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct)
    {
        var dailyTable = ResolveDailyTable(ctx);
        var trunc = OverviewTrafficDailyPostgresSql.ResolveTruncUnit(grouping);
        var bucketExpr = OverviewTrafficDailyPostgresSql.BucketStartSql(trunc, "f.\"DayUtc\"");
        var sql = $"""
                   WITH {OverviewTrafficDailyPostgresSql.BuildFilteredDailyCte(dailyTable)},
                   bucketed AS (
                       SELECT
                           {bucketExpr} AS bucket_ts,
                           f."SessionId",
                           f."TrafficInBytes" AS d_in,
                           f."TrafficOutBytes" AS d_out
                       FROM filtered f
                   )
                   SELECT
                       b.bucket_ts AS "BucketTs",
                       COUNT(DISTINCT b."SessionId")::int AS "ActiveClients",
                       COALESCE(SUM(b.d_in), 0)::bigint AS "TrafficInBytes",
                       COALESCE(SUM(b.d_out), 0)::bigint AS "TrafficOutBytes"
                   FROM bucketed b
                   GROUP BY b.bucket_ts
                   ORDER BY b.bucket_ts
                   """;

        return await ctx.Database
            .SqlQueryRaw<OverviewTrafficBucketRow>(
                sql,
                OverviewTrafficDailyPostgresSql.BuildFilterParams(fromDay, toDayExclusive, vpnServerId, externalId))
            .ToListAsync(ct);
    }

    internal static async Task<IReadOnlyList<OverviewUsersSeriesBucketRow>> QueryUsersSeriesAsync(
        ApplicationDbContext ctx,
        DateOnly fromDay,
        DateOnly toDayExclusive,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct)
    {
        var dailyTable = ResolveDailyTable(ctx);
        var trunc = OverviewTrafficDailyPostgresSql.ResolveTruncUnit(grouping);
        var bucketExpr = OverviewTrafficDailyPostgresSql.BucketStartSql(trunc, "f.\"DayUtc\"");
        var sql = $"""
                   WITH {OverviewTrafficDailyPostgresSql.BuildFilteredDailyCte(dailyTable)},
                   bucketed AS (
                       SELECT
                           {bucketExpr} AS bucket_ts,
                           f."SessionId",
                           NULLIF(f."ExternalId", '') AS external_id
                       FROM filtered f
                   )
                   SELECT
                       b.bucket_ts AS "BucketTs",
                       COUNT(DISTINCT b."SessionId")::int AS "ActiveSessions",
                       COUNT(DISTINCT b.external_id)::int AS "ActiveUsers"
                   FROM bucketed b
                   GROUP BY b.bucket_ts
                   ORDER BY b.bucket_ts
                   """;

        return await ctx.Database
            .SqlQueryRaw<OverviewUsersSeriesBucketRow>(
                sql,
                OverviewTrafficDailyPostgresSql.BuildFilterParams(fromDay, toDayExclusive, vpnServerId, externalId))
            .ToListAsync(ct);
    }

    internal static async Task<OverviewTrafficTotalsRow> QueryTotalsAsync(
        ApplicationDbContext ctx,
        DateOnly fromDay,
        DateOnly toDayExclusive,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct)
    {
        var dailyTable = ResolveDailyTable(ctx);
        var sql = $"""
                   WITH {OverviewTrafficDailyPostgresSql.BuildFilteredDailyCte(dailyTable)}
                   SELECT
                       COALESCE(SUM(f."TrafficInBytes"), 0)::bigint AS "TrafficInBytes",
                       COALESCE(SUM(f."TrafficOutBytes"), 0)::bigint AS "TrafficOutBytes"
                   FROM filtered f
                   """;

        var rows = await ctx.Database
            .SqlQueryRaw<OverviewTrafficTotalsRow>(
                sql,
                OverviewTrafficDailyPostgresSql.BuildFilterParams(fromDay, toDayExclusive, vpnServerId, externalId))
            .ToListAsync(ct);

        return rows[0];
    }

    internal static async Task<IReadOnlyList<OverviewUserTrafficRow>> QueryUserTrafficAsync(
        ApplicationDbContext ctx,
        DateOnly fromDay,
        DateOnly toDayExclusive,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct)
    {
        var dailyTable = ResolveDailyTable(ctx);
        var sql = $"""
                   WITH {OverviewTrafficDailyPostgresSql.BuildFilteredDailyCte(dailyTable)}
                   SELECT
                       f."ExternalId" AS "ExternalId",
                       CASE WHEN COUNT(DISTINCT f."VpnServerId") = 1 THEN MIN(f."VpnServerId") END AS "VpnServerId",
                       COUNT(DISTINCT f."SessionId")::int AS "Sessions",
                       COALESCE(SUM(f."TrafficInBytes"), 0)::bigint AS "TrafficInBytes",
                       COALESCE(SUM(f."TrafficOutBytes"), 0)::bigint AS "TrafficOutBytes",
                       MIN(f."DayUtc")::timestamptz AS "FirstSeen",
                       MAX(f."DayUtc")::timestamptz AS "LastSeen"
                   FROM filtered f
                   GROUP BY f."ExternalId"
                   ORDER BY COALESCE(SUM(f."TrafficInBytes"), 0) + COALESCE(SUM(f."TrafficOutBytes"), 0) DESC, f."ExternalId"
                   """;

        return await ctx.Database
            .SqlQueryRaw<OverviewUserTrafficRow>(
                sql,
                OverviewTrafficDailyPostgresSql.BuildFilterParams(fromDay, toDayExclusive, vpnServerId, externalId))
            .ToListAsync(ct);
    }

    internal static OverviewTrafficTotalsRow CombineTotals(
        OverviewTrafficTotalsRow a,
        OverviewTrafficTotalsRow b)
        => new()
        {
            TrafficInBytes = a.TrafficInBytes + b.TrafficInBytes,
            TrafficOutBytes = a.TrafficOutBytes + b.TrafficOutBytes
        };

    internal static List<OverviewTrafficBucketRow> MergeBuckets(
        IEnumerable<OverviewTrafficBucketRow> first,
        IEnumerable<OverviewTrafficBucketRow> second)
        => first.Concat(second).OrderBy(x => x.BucketTs).ToList();

    private static DateTimeOffset StartOfUtcDay(DateTimeOffset t)
        => new(t.UtcDateTime.Date, TimeSpan.Zero);

    private static string ResolveDailyTable(ApplicationDbContext ctx)
    {
        var entity = ctx.Model.FindEntityType(typeof(VpnServerClientTrafficDaily))
                     ?? throw new InvalidOperationException("VpnServerClientTrafficDaily entity not mapped.");
        var schema = entity.GetSchema() ?? "public";
        var table = entity.GetTableName() ?? throw new InvalidOperationException("Daily table name missing.");
        return $"\"{schema}\".\"{table}\"";
    }
}
