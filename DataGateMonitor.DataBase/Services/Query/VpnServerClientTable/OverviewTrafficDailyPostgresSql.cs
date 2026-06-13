using Npgsql;
using NpgsqlTypes;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public static class OverviewTrafficDailyPostgresSql
{
    public static NpgsqlParameter[] BuildFilterParams(
        DateOnly fromDayUtc,
        DateOnly toDayUtcExclusive,
        int? vpnServerId,
        string? externalId)
    {
        return
        [
            new("fromDay", NpgsqlDbType.Date) { Value = fromDayUtc },
            new("toDay", NpgsqlDbType.Date) { Value = toDayUtcExclusive },
            new("vpnServerId", NpgsqlDbType.Integer) { Value = (object?)vpnServerId ?? DBNull.Value },
            new("externalId", NpgsqlDbType.Text) { Value = (object?)externalId ?? DBNull.Value },
        ];
    }

    public const string VpnServerFilterSql = """(@vpnServerId IS NULL OR d."VpnServerId" = @vpnServerId)""";
    public const string ExternalIdFilterSql = """(@externalId IS NULL OR d."ExternalId" = @externalId)""";

    public static string BuildFilteredDailyCte(string dailyTable)
        => $"""
           filtered AS (
               SELECT
                   d."VpnServerId",
                   d."ExternalId",
                   d."SessionId",
                   d."DayUtc",
                   d."TrafficInBytes",
                   d."TrafficOutBytes"
               FROM {dailyTable} d
               WHERE d."DayUtc" >= @fromDay
                 AND d."DayUtc" < @toDay
                 AND {VpnServerFilterSql}
                 AND {ExternalIdFilterSql}
           )
           """;

    public static string ResolveTruncUnit(OverviewGrouping grouping) => grouping switch
    {
        OverviewGrouping.Hours => "hour",
        OverviewGrouping.Days => "day",
        OverviewGrouping.Months => "month",
        OverviewGrouping.Years => "year",
        _ => "day",
    };

    public static string BucketStartSql(string truncUnit, string dayColumn = "f.\"DayUtc\"")
    {
        if (truncUnit == "day")
            return $"({dayColumn}::timestamp AT TIME ZONE 'UTC')";

        // Build UTC bucket starts from calendar DayUtc (avoids Npgsql/FillMissing key drift on date_trunc).
        return truncUnit switch
        {
            "month" => $"make_timestamptz(EXTRACT(YEAR FROM {dayColumn})::int, EXTRACT(MONTH FROM {dayColumn})::int, 1, 0, 0, 0, 'UTC')",
            "year" => $"make_timestamptz(EXTRACT(YEAR FROM {dayColumn})::int, 1, 1, 0, 0, 0, 'UTC')",
            _ => $"({dayColumn}::timestamp AT TIME ZONE 'UTC')",
        };
    }
}
