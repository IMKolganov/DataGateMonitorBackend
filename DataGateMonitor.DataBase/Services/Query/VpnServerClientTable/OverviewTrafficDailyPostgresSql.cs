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
        => truncUnit == "day"
            ? $"({dayColumn}::timestamp AT TIME ZONE 'UTC')"
            : $"date_trunc('{truncUnit}', {dayColumn}::timestamp AT TIME ZONE 'UTC')";
}
