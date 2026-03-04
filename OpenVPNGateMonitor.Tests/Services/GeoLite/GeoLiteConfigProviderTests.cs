using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenVPNGateMonitor.Services.GeoLite;
using OpenVPNGateMonitor.Services.Others;

namespace OpenVPNGateMonitor.Tests.Services.GeoLite;

public class GeoLiteConfigProviderTests
{
    private static IServiceProvider BuildProvider(string? path, string type = "string")
    {
        var services = new ServiceCollection();
        var settings = new Mock<ISettingsService>(MockBehavior.Strict);
        settings.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);
        if (path != null)
            settings.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path", It.IsAny<CancellationToken>()))
                .ReturnsAsync(path);
        services.AddScoped(_ => settings.Object);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetDatabasePathAsync_Returns_Value_From_Settings()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "GeoLite", "db.mmdb");
        var sp = BuildProvider(tmp);
        var sut = new GeoLiteConfigProvider(sp);

        var path = await sut.GetDatabasePathAsync(CancellationToken.None);
        Assert.Equal(tmp, path);
    }

    [Fact]
    public async Task GetDatabasePathAsync_Throws_When_Type_Not_String()
    {
        var sp = BuildProvider(null, type: "int");
        var sut = new GeoLiteConfigProvider(sp);
        await Assert.ThrowsAsync<Exception>(() => sut.GetDatabasePathAsync(CancellationToken.None));
    }

    [Fact]
    public void CreateTimestamp_Produces_Expected_Format()
    {
        var sut = new GeoLiteConfigProvider(BuildProvider("/tmp/x"));
        var ts = sut.CreateTimestamp();
        Assert.Matches("^\\d{8}_\\d{6}$", ts);
    }

    [Fact]
    public void PreparePaths_Creates_Directories_And_Formats_Paths()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "GeoLiteTest_" + Guid.NewGuid().ToString("N"));
        var dbPath = Path.Combine(baseDir, "GeoLite2-City.mmdb");
        var sut = new GeoLiteConfigProvider(BuildProvider(dbPath));
        var ts = "20240101_010203";

        var (b, extract, temp) = sut.PreparePaths(dbPath, ts);

        Assert.Equal(baseDir, b);
        Assert.True(Directory.Exists(b));
        Assert.True(Directory.Exists(extract));
        Assert.EndsWith($"GeoLite2_{ts}", extract);
        Assert.EndsWith($"GeoLite2-City_{ts}.tar.gz", temp);

        // Cleanup
        Directory.Delete(b, recursive: true);
    }
}
