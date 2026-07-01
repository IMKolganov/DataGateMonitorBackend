using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.VpnServerEventLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerEventLogTable;

public class VpnServerEventLogQueryServiceTests
{
    [Fact]
    public async Task GetAppVersionSummaryAsync_GroupsByIvGuiVer_AndReturnsLastConnectTime()
    {
        var t1 = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero);

        await using var context = CreateContext();
        context.VpnServerEventLogs.AddRange(
            Event(1, "cn-a", "ClientConnected", "3.12_datagate_android_1.0.7", t1),
            Event(2, "cn-a", "ClientConnected", "3.12_datagate_android_1.0.7", t2),
            Event(3, "cn-b", "ClientConnected", "3.12_datagate_windows_1.0.6", t3),
            Event(4, "cn-a", "ClientDisconnect", "3.12_datagate_android_1.0.7", t2),
            Event(5, "cn-a", "ClientConnected", null, t2),
            Event(6, "cn-other", "ClientConnected", "3.12_datagate_android_1.0.7", t2));

        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnServerEventLog)] = new TestQuery<VpnServerEventLog>(context.VpnServerEventLogs),
        };
        var sut = new VpnServerEventLogQueryService(new EfQueryService<VpnServerEventLog, int>(new TestUnitOfWork(queries)));

        var items = await sut.GetAppVersionSummaryAsync(75, ["cn-a", "cn-b"], CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal("3.12_datagate_windows_1.0.6", items[0].IvGuiVer);
        Assert.Equal(t3, items[0].LastConnectedAtUtc);
        Assert.Equal(1, items[0].ConnectionCount);
        Assert.Equal("3.12_datagate_android_1.0.7", items[1].IvGuiVer);
        Assert.Equal(t2, items[1].LastConnectedAtUtc);
        Assert.Equal(2, items[1].ConnectionCount);
    }

    [Fact]
    public async Task GetAppVersionSummaryAsync_WithSingleCommonName_FiltersProfiles()
    {
        var t1 = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

        await using var context = CreateContext();
        context.VpnServerEventLogs.AddRange(
            Event(1, "cn-a", "ClientConnect", "3.12_datagate_android_1.0.7", t1),
            Event(2, "cn-b", "ClientConnect", "3.12_datagate_windows_1.0.6", t2));

        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnServerEventLog)] = new TestQuery<VpnServerEventLog>(context.VpnServerEventLogs),
        };
        var sut = new VpnServerEventLogQueryService(new EfQueryService<VpnServerEventLog, int>(new TestUnitOfWork(queries)));

        var items = await sut.GetAppVersionSummaryAsync(75, ["cn-a"], CancellationToken.None);

        Assert.Single(items);
        Assert.Equal("3.12_datagate_android_1.0.7", items[0].IvGuiVer);
    }

    [Fact]
    public async Task GetAppVersionSummaryAsync_FallsBackToIvVer_WhenIvGuiVerMissing()
    {
        var t1 = new DateTimeOffset(2026, 6, 28, 8, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 6, 29, 1, 53, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);

        await using var context = CreateContext();
        context.VpnServerEventLogs.AddRange(
            Event(1, "cn-a", "ClientConnected", ivGuiVer: null, ivVer: "3.12_datagate_android_1.0.6", t1),
            Event(2, "cn-a", "ClientConnected", ivGuiVer: null, ivVer: "3.12_datagate_android_1.0.6", t2),
            Event(3, "cn-a", "ClientConnected", ivGuiVer: "3.12_datagate_android_1.0.7", ivVer: "3.11.5", t3));

        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnServerEventLog)] = new TestQuery<VpnServerEventLog>(context.VpnServerEventLogs),
        };
        var sut = new VpnServerEventLogQueryService(new EfQueryService<VpnServerEventLog, int>(new TestUnitOfWork(queries)));

        var items = await sut.GetAppVersionSummaryAsync(75, ["cn-a"], CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal("3.12_datagate_android_1.0.7", items[0].IvGuiVer);
        Assert.Equal(1, items[0].ConnectionCount);
        Assert.Equal(t3, items[0].LastConnectedAtUtc);
        Assert.Equal("3.12_datagate_android_1.0.6", items[1].IvGuiVer);
        Assert.Equal(2, items[1].ConnectionCount);
        Assert.Equal(t2, items[1].LastConnectedAtUtc);
    }

    private static VpnServerEventLog Event(
        int id,
        string commonName,
        string eventType,
        string? ivGuiVer,
        DateTimeOffset eventTimeUtc,
        string? ivVer = null)
        => Event(id, commonName, eventType, ivGuiVer, ivVer, eventTimeUtc);

    private static VpnServerEventLog Event(
        int id,
        string commonName,
        string eventType,
        string? ivGuiVer,
        string? ivVer,
        DateTimeOffset eventTimeUtc)
        => new()
        {
            Id = id,
            VpnServerId = 75,
            CommonName = commonName,
            EventType = eventType,
            IvGuiVer = ivGuiVer,
            IvVer = ivVer,
            EventTimeUtc = eventTimeUtc,
            CreateDate = eventTimeUtc,
            LastUpdate = eventTimeUtc,
        };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataBaseSettings:DefaultSchema"] = "test_schema" })
            .Build();
        return new ApplicationDbContext(options, configuration);
    }
}
