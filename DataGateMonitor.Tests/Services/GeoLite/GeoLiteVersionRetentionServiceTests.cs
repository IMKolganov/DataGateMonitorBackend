using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.Others;
using Xunit;

namespace DataGateMonitor.Tests.Services.GeoLite;

public class GeoLiteVersionRetentionServiceTests
{
    private static IServiceScopeFactory ScopeWithMaxKept(int maxKept)
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetValueAsync<int>(GeoLiteRetentionSettings.MaxKeptVersionsKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(maxKept);
        var sc = new ServiceCollection();
        sc.AddScoped(_ => settings.Object);
        return sc.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(5, 5)]
    [InlineData(200, 100)]
    public void ResolveMaxKept_Clamps_And_Defaults(int input, int expected)
        => Assert.Equal(expected, GeoLiteRetentionSettings.ResolveMaxKept(input));

    [Fact]
    public async Task PruneOldVersionsAsync_Keeps_Newest_N_Directories()
    {
        var root = Path.Combine(Path.GetTempPath(), "GeoLiteRetention_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var dbPath = Path.Combine(root, "geo-lite2-city.mmdb");
        await File.WriteAllTextAsync(dbPath, "x");

        for (var i = 1; i <= 5; i++)
            Directory.CreateDirectory(Path.Combine(root, $"GeoLite2_2026010{i}_120000"));

        try
        {
            var sut = new GeoLiteVersionRetentionService(
                ScopeWithMaxKept(2),
                NullLogger<GeoLiteVersionRetentionService>.Instance);

            var removed = await sut.PruneOldVersionsAsync(dbPath, CancellationToken.None);

            Assert.Equal(3, removed);
            var remaining = Directory.GetDirectories(root, "GeoLite2_*").Select(Path.GetFileName).OrderBy(x => x).ToArray();
            Assert.Equal(new[] { "GeoLite2_20260104_120000", "GeoLite2_20260105_120000" }, remaining);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task PruneOldVersionsAsync_ExplicitKeepMinusOne_FreesSlotAtCapacity()
    {
        var root = Path.Combine(Path.GetTempPath(), "GeoLiteRetention_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var dbPath = Path.Combine(root, "geo-lite2-city.mmdb");
        await File.WriteAllTextAsync(dbPath, "x");

        for (var i = 1; i <= 10; i++)
            Directory.CreateDirectory(Path.Combine(root, $"GeoLite2_202601{i:00}_120000"));

        try
        {
            var sut = new GeoLiteVersionRetentionService(
                ScopeWithMaxKept(10),
                NullLogger<GeoLiteVersionRetentionService>.Instance);

            // At capacity: default prune would no-op; pre-download must pass keep-1.
            Assert.Equal(0, await sut.PruneOldVersionsAsync(dbPath, CancellationToken.None));
            Assert.Equal(1, await sut.PruneOldVersionsAsync(dbPath, CancellationToken.None, maxKept: 9));
            Assert.Equal(9, Directory.GetDirectories(root, "GeoLite2_*").Length);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void IsDiskFull_Detects_NoSpaceLeft_Message()
    {
        var ex = new IOException("No space left on device : '/tmp/ASPNETCORE_x.tmp'");
        Assert.True(GeoLiteDiskFull.IsDiskFull(ex));
        Assert.Contains("No space left", GeoLiteDiskFull.FormatDetail(ex), StringComparison.OrdinalIgnoreCase);
    }
}
