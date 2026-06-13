using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.Others;

namespace DataGateMonitor.Tests.Services.GeoLite;

public class GeoLiteQueryServiceTests
{
    [Fact]
    public void GetDatabasePath_Delegates_To_Factory()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var factory = new GeoLiteDatabaseFactory(new NullLogger<GeoLiteDatabaseFactory>(), sp);
        var field = typeof(GeoLiteDatabaseFactory).GetField("_dbPath", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(factory, "/tmp/db.mmdb");
        var sut = new GeoLiteQueryService(factory, new NullLogger<GeoLiteQueryService>());

        var path = sut.GetDatabasePath();
        Assert.Equal("/tmp/db.mmdb", path);
    }

    [Fact]
    public async Task GetDatabaseVersionAsync_Returns_Error_When_Factory_Fails()
    {
        // Configure factory to point to a non-existent file so loading fails
        var services = new ServiceCollection();
        var settings = new Moq.Mock<ISettingsService>(MockBehavior.Strict);
        settings.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("string");
        settings.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.mmdb"));
        services.AddScoped(_ => settings.Object);
        var sp = services.BuildServiceProvider();

        var factory = new GeoLiteDatabaseFactory(new NullLogger<GeoLiteDatabaseFactory>(), sp);
        var sut = new GeoLiteQueryService(factory, new NullLogger<GeoLiteQueryService>());

        var version = await sut.GetDatabaseVersionAsync(CancellationToken.None);
        Assert.StartsWith("Error retrieving version.", version);
    }

    [Theory]
    [InlineData("10.0.0.1:1194")]
    [InlineData("192.168.1.10")] 
    [InlineData("172.16.0.5:443")] 
    public async Task GetGeoInfoAsync_Returns_Rfc1918_For_Private_Ipv4(string host)
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var factory = new GeoLiteDatabaseFactory(new NullLogger<GeoLiteDatabaseFactory>(), sp);
        var sut = new GeoLiteQueryService(factory, new NullLogger<GeoLiteQueryService>());

        var info = await sut.GetGeoInfoAsync(host, CancellationToken.None);
        Assert.NotNull(info);
        Assert.Equal("Internet", info!.Country);
        Assert.Equal("RFC1918", info.Region);
        Assert.Equal("RFC1918", info.City);
    }

    [Theory]
    [InlineData("[fe80::1]:1194")] // link-local
    [InlineData("ff02::1")] // multicast
    public async Task GetGeoInfoAsync_Returns_Null_For_Special_Ipv6(string host)
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var factory = new GeoLiteDatabaseFactory(new NullLogger<GeoLiteDatabaseFactory>(), sp);
        var sut = new GeoLiteQueryService(factory, new NullLogger<GeoLiteQueryService>());

        var info = await sut.GetGeoInfoAsync(host, CancellationToken.None);
        Assert.Null(info);
    }
}
