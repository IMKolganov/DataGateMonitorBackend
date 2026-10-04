using System.Diagnostics;
using System.Globalization;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

/// <summary>
/// Validates concurrent peak/low SQL semantics against prod dump in Docker (<c>datagate-dump-pg</c>).
/// Snapshot: backups/datagate_db_prod_backup_20260929_213717.dump (max MeasuredAt 2026-09-29).
/// </summary>
public class OverviewConcurrentExtremesProdDumpTests
{
    private const string DockerContainer = "datagate-dump-pg";
    private const string DbUser = "datagate";
    private const string DbName = "datagate_db_prod";

    [SkippableFact]
    public void ProdDump_2026_09_28_Hourly_Users_Peak38_Low15()
    {
        Skip.IfNot(CanQueryDump(), "Docker container datagate-dump-pg is not available / empty.");

        // date_trunc('hour') + COUNT(DISTINCT ExternalId) — same shape as users-series buckets.
        const string sql = """
            WITH bounds AS (
              SELECT TIMESTAMPTZ '2026-09-28 00:00:00+00' AS from_ts,
                     TIMESTAMPTZ '2026-09-29 00:00:00+00' AS to_ts
            ),
            bucketed AS (
              SELECT
                date_trunc('hour', t."MeasuredAt") AS bucket_ts,
                COUNT(DISTINCT NULLIF(t."ExternalId", ''))::int AS active_users
              FROM xgb_dashopnvpn."VpnServerClientTraffics" t, bounds b
              WHERE t."MeasuredAt" >= b.from_ts AND t."MeasuredAt" < b.to_ts
              GROUP BY 1
            )
            SELECT
              (SELECT COUNT(DISTINCT NULLIF("ExternalId",''))
                 FROM xgb_dashopnvpn."VpnServerClientTraffics" t, bounds b
                 WHERE t."MeasuredAt" >= b.from_ts AND t."MeasuredAt" < b.to_ts) AS unique_users,
              (SELECT MAX(active_users) FROM bucketed) AS peak_users,
              (SELECT MIN(active_users) FROM bucketed) AS low_users,
              (SELECT bucket_ts FROM bucketed ORDER BY active_users DESC, bucket_ts LIMIT 1) AS peak_at,
              (SELECT bucket_ts FROM bucketed ORDER BY active_users ASC, bucket_ts LIMIT 1) AS low_at
            ;
            """;

        var parts = QueryFields(sql);
        var uniqueUsers = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var peakUsers = int.Parse(parts[1], CultureInfo.InvariantCulture);
        var lowUsers = int.Parse(parts[2], CultureInfo.InvariantCulture);

        Assert.Equal(67, uniqueUsers);
        Assert.Equal(38, peakUsers);
        Assert.Equal(15, lowUsers);
        Assert.True(peakUsers < uniqueUsers);
        Assert.True(lowUsers > 0);
        Assert.True(lowUsers < peakUsers);
        Assert.False(string.IsNullOrWhiteSpace(parts[3]));
        Assert.False(string.IsNullOrWhiteSpace(parts[4]));
    }

    [SkippableFact]
    public void ProdDump_2026_09_29_IncompleteDay_ZeroFill_Would_Pull_Low_To_Zero()
    {
        Skip.IfNot(CanQueryDump(), "Docker container datagate-dump-pg is not available / empty.");

        const string sql = """
            WITH bounds AS (
              SELECT TIMESTAMPTZ '2026-09-29 00:00:00+00' AS from_ts,
                     TIMESTAMPTZ '2026-09-30 00:00:00+00' AS to_ts
            ),
            bucketed AS (
              SELECT
                date_trunc('hour', t."MeasuredAt") AS bucket_ts,
                COUNT(DISTINCT NULLIF(t."ExternalId", ''))::int AS active_users
              FROM xgb_dashopnvpn."VpnServerClientTraffics" t, bounds b
              WHERE t."MeasuredAt" >= b.from_ts AND t."MeasuredAt" < b.to_ts
              GROUP BY 1
            )
            SELECT
              (SELECT COUNT(*) FROM bucketed) AS buckets_with_samples,
              (SELECT MAX(active_users) FROM bucketed) AS peak_users,
              (SELECT MIN(active_users) FROM bucketed) AS low_users_raw,
              (SELECT MIN(active_users) FROM (
                 SELECT COALESCE(b.active_users, 0) AS active_users
                 FROM bounds bb
                 CROSS JOIN generate_series(bb.from_ts, bb.to_ts - interval '1 hour', interval '1 hour') gs
                 LEFT JOIN bucketed b ON b.bucket_ts = gs
               ) filled) AS low_users_filled
            ;
            """;

        var parts = QueryFields(sql);
        Assert.Equal(22, int.Parse(parts[0], CultureInfo.InvariantCulture));
        Assert.Equal(35, int.Parse(parts[1], CultureInfo.InvariantCulture));
        Assert.Equal(14, int.Parse(parts[2], CultureInfo.InvariantCulture));
        Assert.Equal(0, int.Parse(parts[3], CultureInfo.InvariantCulture));
    }

    private static string[] QueryFields(string sql)
    {
        var lines = ExecutePsql(sql);
        Assert.NotEmpty(lines);
        return lines[0].Split('|');
    }

    private static List<string> ExecutePsql(string sql)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"exec -i {DockerContainer} psql -U {DbUser} -d {DbName} -t -A -F|",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        process.StandardInput.WriteLine(sql);
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEnd();
        var err = process.StandardError.ReadToEnd();
        process.WaitForExit(60_000);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"psql failed: {err}");

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
    }

    private static bool CanQueryDump()
    {
        try
        {
            var lines = ExecutePsql("""SELECT COUNT(*) FROM xgb_dashopnvpn."VpnServerClientTraffics";""");
            return lines.Count > 0
                   && long.TryParse(lines[0], CultureInfo.InvariantCulture, out var n)
                   && n > 0;
        }
        catch
        {
            return false;
        }
    }
}
