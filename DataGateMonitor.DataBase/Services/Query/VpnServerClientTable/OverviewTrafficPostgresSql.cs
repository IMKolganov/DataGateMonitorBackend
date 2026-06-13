using Npgsql;
using NpgsqlTypes;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

/// <summary>
/// PostgreSQL text and parameters for overview traffic aggregation.
/// Extracted so parameter typing is unit-tested (42P08 when nullable filters are untyped).
/// </summary>
public static class OverviewTrafficPostgresSql
{
    public const string VpnServerFilterSql = """(@vpnServerId IS NULL OR t."VpnServerId" = @vpnServerId)""";
    public const string ExternalIdFilterSql = """(@externalId IS NULL OR t."ExternalId" = @externalId)""";

    public static NpgsqlParameter[] BuildFilterParams(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        TimeSpan offset,
        int? vpnServerId,
        string? externalId)
    {
        return
        [
            new("from", NpgsqlDbType.TimestampTz) { Value = fromUtc.UtcDateTime },
            new("to", NpgsqlDbType.TimestampTz) { Value = toUtc.UtcDateTime },
            new("offset", NpgsqlDbType.Interval) { Value = offset },
            new("vpnServerId", NpgsqlDbType.Integer) { Value = (object?)vpnServerId ?? DBNull.Value },
            new("externalId", NpgsqlDbType.Text) { Value = (object?)externalId ?? DBNull.Value },
        ];
    }

    public static string BuildFilteredTrafficCte(string table)
        => $"""
           filtered AS (
               SELECT
                   t."SessionId",
                   t."ExternalId",
                   t."VpnServerId",
                   t."MeasuredAt",
                   t."BytesReceived",
                   t."BytesSent"
               FROM {table} t
               WHERE t."MeasuredAt" >= @from
                 AND t."MeasuredAt" < @to
                 AND {VpnServerFilterSql}
                 AND {ExternalIdFilterSql}
           )
           """;

    public static string BucketStartSql(string truncUnit)
        => $"""((date_trunc('{truncUnit}', ("MeasuredAt" AT TIME ZONE 'UTC') + @offset::interval) AT TIME ZONE 'UTC') - @offset::interval)::timestamptz""";
}
