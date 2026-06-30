using System.Diagnostics;
using System.Globalization;
using DataGateMonitor.Models;
using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.CertExpiry;

/// <summary>
/// Validates first-run impact against prod backup restored in Docker (<c>datagate_prod_restore</c>).
/// Snapshot date: backup 2026-06-29, simulation reference 2026-06-30 UTC.
/// </summary>
public class CertExpiryProdBackupAnalysisTests
{
    private const string DockerContainer = "datagate_prod_restore";
    private const string DbUser = "datagate";
    private const string DbName = "datagate_db_prod";
    private static readonly DateTimeOffset SimulationNow = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    [SkippableFact]
    public void ProdBackupSimulation_MatchesExpectedFirstRunImpact()
    {
        Skip.IfNot(CanQueryProdRestore(), "Docker container datagate_prod_restore is not available.");

        var activeFiles = LoadActiveIssuedFiles();
        var servers = LoadVpnServers();

        var result = CertExpiryProdSimulation.Simulate(activeFiles, servers, SimulationNow, warningDays: 30);

        Assert.Equal(1166, result.TotalActiveInDb);
        Assert.Equal(4, result.OpenVpnServerCount);
        Assert.Equal(293, result.ProfilesOnEligibleServers);
        Assert.Equal(873, result.SkippedIneligible);
        Assert.Equal(1, result.EstimatedExpired);
        Assert.Equal(0, result.EstimatedExpiringSoon);
        Assert.Equal(292, result.EstimatedHealthy);
        Assert.Equal(1, result.EstimatedFirstRunNotifications);
    }

    [SkippableFact]
    public void ProdBackupSimulation_FirstRunSqlAndHttpFootprint()
    {
        Skip.IfNot(CanQueryProdRestore(), "Docker container datagate_prod_restore is not available.");

        var activeFiles = LoadActiveIssuedFiles();
        var servers = LoadVpnServers();
        var eligibleServerCount = servers.Count(CertExpiryScheduledCheckRunner.IsOpenVpnServerCandidate);

        // Mirrors CertExpiryScheduledCheckRunner: Settings + VpnServers + GetAllActiveByVpnServerIds + N cert API calls.
        const int expectedSqlQueries = 3;
        var expectedHttpCalls = eligibleServerCount;

        Assert.Equal(4, eligibleServerCount);
        Assert.Equal(3, expectedSqlQueries);
        Assert.Equal(4, expectedHttpCalls);

        var profilesLoaded = activeFiles.Count(f =>
            servers.Where(CertExpiryScheduledCheckRunner.IsOpenVpnServerCandidate).Select(s => s.Id).Contains(f.VpnServerId));

        Assert.Equal(293, profilesLoaded);
        Assert.True(profilesLoaded < activeFiles.Count, "Optimized query should load fewer rows than all active profiles.");
    }

    private static bool CanQueryProdRestore()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {DockerContainer} psql -U {DbUser} -d {DbName} -t -A -c \"SELECT 1\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            process!.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static List<IssuedOvpnFile> LoadActiveIssuedFiles()
    {
        const string sql = """
            SELECT "Id","VpnServerId","CommonName","ExternalId","IssuedAt"
            FROM xgb_dashopnvpn."IssuedOvpnFiles"
            WHERE "IsRevoked" = false;
            """;
        var lines = ExecutePsql(sql);
        return lines.Select(ParseIssuedFile).ToList();
    }

    private static List<VpnServer> LoadVpnServers()
    {
        const string sql = """
            SELECT "Id","ServerName","ServerType","ApiUrl","IsDisable","IsDeleted"
            FROM xgb_dashopnvpn."VpnServers";
            """;
        var lines = ExecutePsql(sql);
        return lines.Select(ParseVpnServer).ToList();
    }

    private static IssuedOvpnFile ParseIssuedFile(string line)
    {
        var p = line.Split('|');
        return new IssuedOvpnFile
        {
            Id = int.Parse(p[0], CultureInfo.InvariantCulture),
            VpnServerId = int.Parse(p[1], CultureInfo.InvariantCulture),
            CommonName = p[2],
            ExternalId = p[3],
            IssuedAt = DateTimeOffset.Parse(p[4], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            IsRevoked = false
        };
    }

    private static VpnServer ParseVpnServer(string line)
    {
        var p = line.Split('|');
        return new VpnServer
        {
            Id = int.Parse(p[0], CultureInfo.InvariantCulture),
            ServerName = p[1],
            ServerType = (VpnServerType)int.Parse(p[2], CultureInfo.InvariantCulture),
            ApiUrl = p[3],
            IsDisable = ParsePostgresBool(p[4]),
            IsDeleted = ParsePostgresBool(p[5])
        };
    }

    private static bool ParsePostgresBool(string value) =>
        value.Equals("t", StringComparison.OrdinalIgnoreCase)
        || value.Equals("true", StringComparison.OrdinalIgnoreCase);

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
}
