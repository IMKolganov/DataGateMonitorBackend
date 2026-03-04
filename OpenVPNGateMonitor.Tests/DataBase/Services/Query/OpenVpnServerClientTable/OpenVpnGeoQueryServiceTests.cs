using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Tests.Helpers;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.OpenVpnServerClientTable;

public class OpenVpnGeoQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<OpenVpnServerClient> Clients => Set<OpenVpnServerClient>();
    }

    private static (Mock<IUnitOfWork> uow, TestDbContext ctx) CreateUowWithData(IEnumerable<OpenVpnServerClient> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.Clients.AddRange(data);
        ctx.SaveChanges();

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.GetQuery<OpenVpnServerClient>())
           .Returns(new TestQuery<OpenVpnServerClient>(ctx.Clients));
        return (uow, ctx);
    }

    [Fact]
    public async Task GetGeoPointsAsync_Filters_By_Time_And_Server_And_Excludes_Zero_Coord_When_Flag_True()
    {
        var now = DateTimeOffset.UtcNow;
        var from = now.AddHours(-6);
        var to = now.AddHours(1);

        var data = new List<OpenVpnServerClient>
        {
            new() { Id = 1, VpnServerId = 1, ExternalId = "e1", ConnectedSince = now.AddHours(-5), Country = "US", Region = "CA", Latitude = 37.77, Longitude = -122.41, BytesReceived = 100, BytesSent = 200 },
            new() { Id = 2, VpnServerId = 1, ExternalId = "e1", ConnectedSince = now.AddHours(-4), Country = "US", Region = "CA", Latitude = 37.77, Longitude = -122.41, BytesReceived = 50,  BytesSent = 70 },
            new() { Id = 3, VpnServerId = 2, ExternalId = "e2", ConnectedSince = now.AddHours(-3), Country = "DE", Region = "BE", Latitude = 52.52, Longitude = 13.405, BytesReceived = 10,  BytesSent = 20 },
            // Outside time range
            new() { Id = 4, VpnServerId = 1, ExternalId = "e1", ConnectedSince = now.AddHours(-10), Country = "US", Region = "CA", Latitude = 37.77, Longitude = -122.41, BytesReceived = 1, BytesSent = 1 },
            // Zero coords should be excluded when onlyWithCoordinates = true
            new() { Id = 5, VpnServerId = 1, ExternalId = "e1", ConnectedSince = now.AddHours(-2), Country = "US", Region = "CA", Latitude = 0, Longitude = 0, BytesReceived = 1000, BytesSent = 1000 },
        };

        var (uow, ctx) = CreateUowWithData(data);
        var sut = new OpenVpnGeoQueryService(uow.Object);

        var res = await sut.GetGeoPointsAsync(from, to, vpnServerId: 1, externalId: "e1", onlyWithCoordinates: true, CancellationToken.None);

        // Expect only points in US/CA with given lat/lon; aggregated
        Assert.Single(res.GeoPointAggs);
        var p = res.GeoPointAggs[0];
        Assert.Equal("US", p.Country);
        Assert.Equal("CA", p.Region);
        Assert.Equal(37.77, p.Latitude);
        Assert.Equal(-122.41, p.Longitude);
        Assert.Equal(2, p.SessionsCount); // Id 1 and 2
        Assert.Equal(150, p.TotalBytesIn);
        Assert.Equal(270, p.TotalBytesOut);

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetGeoPointsAsync_Includes_Zero_Coord_When_Flag_False_And_Normalizes_Reversed_Range()
    {
        var now = DateTimeOffset.UtcNow;
        var from = now.AddHours(-6);
        var to = now.AddHours(1);

        var data = new List<OpenVpnServerClient>
        {
            new() { Id = 1, VpnServerId = 3, ExternalId = "e3", ConnectedSince = now.AddHours(-2), Country = "XX", Region = "YY", Latitude = 0, Longitude = 0, BytesReceived = 5, BytesSent = 7 },
        };

        var (uow, ctx) = CreateUowWithData(data);
        var sut = new OpenVpnGeoQueryService(uow.Object);

        // reversed range
        var res = await sut.GetGeoPointsAsync(to, from, vpnServerId: 3, externalId: "e3", onlyWithCoordinates: false, CancellationToken.None);

        Assert.Single(res.GeoPointAggs);
        var p = res.GeoPointAggs[0];
        Assert.Equal(0, p.Latitude);
        Assert.Equal(0, p.Longitude);
        Assert.Equal(1, p.SessionsCount);
        Assert.Equal(5, p.TotalBytesIn);
        Assert.Equal(7, p.TotalBytesOut);

        await ctx.DisposeAsync();
    }
}
