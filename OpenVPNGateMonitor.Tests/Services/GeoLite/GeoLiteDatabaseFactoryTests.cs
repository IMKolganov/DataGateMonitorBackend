using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenVPNGateMonitor.Services.GeoLite;
using OpenVPNGateMonitor.Services.Others;

namespace OpenVPNGateMonitor.Tests.Services.GeoLite;

public class GeoLiteDatabaseFactoryTests
{
    private static (GeoLiteDatabaseFactory sut, string path) CreateFactory(string? path = null)
    {
        var services = new ServiceCollection();
        var settings = new Mock<ISettingsService>(MockBehavior.Strict);
        var dbPath = path ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "db.mmdb");
        settings.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("string");
        settings.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbPath);
        services.AddScoped(_ => settings.Object);
        var sp = services.BuildServiceProvider();

        var sut = new GeoLiteDatabaseFactory(new NullLogger<GeoLiteDatabaseFactory>(), sp);
        return (sut, dbPath);
    }

    [Fact]
    public async Task GetDatabaseAsync_Throws_When_File_Does_Not_Exist()
    {
        var (sut, _) = CreateFactory();
        await Assert.ThrowsAsync<MaxMind.Db.InvalidDatabaseException>(() => sut.GetDatabaseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetDatabasePath_Throws_When_Not_Loaded()
    {
        var (sut, _) = CreateFactory();
        Assert.False(sut.IsDatabaseLoaded);
        Assert.Throws<InvalidOperationException>(() => sut.GetDatabasePath());
        await sut.DeleteDatabaseAsync(CancellationToken.None); // should not throw
    }
}
