using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class OpenVpnOverviewSeriesQueryTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<VpnServerClientTraffic> Traffic => Set<VpnServerClientTraffic>();
        public DbSet<UserIdentityLink> UserIdentityLinks => Set<UserIdentityLink>();
        public DbSet<User> Users => Set<User>();
    }

    private static (Mock<IUnitOfWork> uow, TestDbContext ctx) CreateUow()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.GetQuery<VpnServerClientTraffic>())
           .Returns(() => new TestQuery<VpnServerClientTraffic>(ctx.Traffic));
        uow.Setup(x => x.GetQuery<UserIdentityLink>())
           .Returns(() => new TestQuery<UserIdentityLink>(ctx.UserIdentityLinks));
        uow.Setup(x => x.GetQuery<User>())
           .Returns(() => new TestQuery<User>(ctx.Users));
        return (uow, ctx);
    }

    [Fact]
    public async Task GetOverviewSeriesFromSessionsAsync_Hours_Computes_Deltas_And_ActiveClients()
    {
        var (uow, ctx) = CreateUow();

        var baseTs = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var from = baseTs;
        var to = baseTs.AddHours(2); // 00:00..02:00 => buckets 00:00 and 01:00

        var sA = Guid.NewGuid();
        var sB = Guid.NewGuid();

        // Session A samples across two hours
        ctx.Traffic.AddRange(new[]
        {
            new VpnServerClientTraffic { Id = 1, VpnServerId = 1, ExternalId = "u1", SessionId = sA, BytesReceived = 0,   BytesSent = 0,  MeasuredAt = baseTs.AddMinutes(10) },
            new VpnServerClientTraffic { Id = 2, VpnServerId = 1, ExternalId = "u1", SessionId = sA, BytesReceived = 100, BytesSent = 50, MeasuredAt = baseTs.AddMinutes(50) },
            new VpnServerClientTraffic { Id = 3, VpnServerId = 1, ExternalId = "u1", SessionId = sA, BytesReceived = 150, BytesSent = 75, MeasuredAt = baseTs.AddHours(1).AddMinutes(20) },
        });

        // Session B single sample within 01:00 bucket (contributes to ActiveClients only)
        ctx.Traffic.Add(
            new VpnServerClientTraffic { Id = 4, VpnServerId = 1, ExternalId = "u2", SessionId = sB, BytesReceived = 5, BytesSent = 7, MeasuredAt = baseTs.AddHours(1).AddMinutes(10) }
        );

        await ctx.SaveChangesAsync();

        var sut = new OpenVpnOverviewSeriesQuery(uow.Object, OverviewQueryTestHelper.CreateTrafficAggregator(uow.Object));
        var res = await sut.GetOverviewSeriesFromSessionsAsync(from, to, OverviewGrouping.Hours, vpnServerId: 1, externalId: null, CancellationToken.None);

        // FillMissingBuckets includes the bucket aligned to 'to' as well => expect 3: 00:00, 01:00, 02:00
        Assert.Equal(3, res.OverviewSeriesRows.Count);

        var b0 = res.OverviewSeriesRows[0];
        var b1 = res.OverviewSeriesRows[1];
        var b2 = res.OverviewSeriesRows[2];

        // First bucket (00:00): deltas 0->100 in, 0->50 out; ActiveClients 1 (only A has samples in 00h)
        Assert.Equal(baseTs, b0.Ts);
        Assert.Equal(1, b0.ActiveClients);
        Assert.Equal(100, b0.TrafficInBytes);
        Assert.Equal(50, b0.TrafficOutBytes);

        // Second bucket (01:00): deltas 100->150 (+50/+25) for A; B has no delta (single sample) but increases ActiveClients
        Assert.Equal(baseTs.AddHours(1), b1.Ts);
        Assert.Equal(2, b1.ActiveClients);
        Assert.Equal(50, b1.TrafficInBytes);
        Assert.Equal(25, b1.TrafficOutBytes);

        // Third bucket (02:00) is zero-filled
        Assert.Equal(baseTs.AddHours(2), b2.Ts);
        Assert.Equal(0, b2.ActiveClients);
        Assert.Equal(0, b2.TrafficInBytes);
        Assert.Equal(0, b2.TrafficOutBytes);

        // Summary reflects totals across buckets
        Assert.Equal(150, res.Summary.TotalTrafficInBytes);
        Assert.Equal(75, res.Summary.TotalTrafficOutBytes);
    }

    [Fact]
    public async Task GetOverviewUsersFromSessionsAsync_Aggregates_By_ExternalId_And_Enriches_DisplayName()
    {
        var (uow, ctx) = CreateUow();
        var baseTs = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var from = baseTs;
        var to = baseTs.AddHours(3);

        var sA = Guid.NewGuid();
        var sB = Guid.NewGuid();
        var sC = Guid.NewGuid();

        // u1 on server 10 only (single server id should remain 10)
        ctx.Traffic.AddRange(new[]
        {
            new VpnServerClientTraffic { Id = 1, VpnServerId = 10, ExternalId = "u1", SessionId = sA, BytesReceived = 0, BytesSent = 0, MeasuredAt = baseTs.AddMinutes(5) },
            new VpnServerClientTraffic { Id = 2, VpnServerId = 10, ExternalId = "u1", SessionId = sA, BytesReceived = 20, BytesSent = 10, MeasuredAt = baseTs.AddMinutes(15) },
        });

        // u2 across two servers (SingleServerId -> null), two sessions
        ctx.Traffic.AddRange(new[]
        {
            new VpnServerClientTraffic { Id = 3, VpnServerId = 20, ExternalId = "u2", SessionId = sB, BytesReceived = 5,  BytesSent = 2, MeasuredAt = baseTs.AddHours(1) },
            new VpnServerClientTraffic { Id = 4, VpnServerId = 30, ExternalId = "u2", SessionId = sC, BytesReceived = 8,  BytesSent = 3, MeasuredAt = baseTs.AddHours(2) },
            new VpnServerClientTraffic { Id = 5, VpnServerId = 30, ExternalId = "u2", SessionId = sC, BytesReceived = 18, BytesSent = 13, MeasuredAt = baseTs.AddHours(2).AddMinutes(30) },
        });
        await ctx.SaveChangesAsync();

        // Enrichment tables
        ctx.UserIdentityLinks.AddRange(new[]
        {
            new UserIdentityLink { Id = 1, ExternalId = "u1", UserId = 100, Provider = "backend" },
            new UserIdentityLink { Id = 2, ExternalId = "u2", UserId = 200, Provider = "backend" },
        });
        ctx.Users.AddRange(new[]
        {
            new User { Id = 100, DisplayName = "User One" },
            new User { Id = 200, DisplayName = "User Two" },
        });
        await ctx.SaveChangesAsync();

        var sut = new OpenVpnOverviewSeriesQuery(uow.Object, OverviewQueryTestHelper.CreateTrafficAggregator(uow.Object));
        var res = await sut.GetOverviewUsersFromSessionsAsync(from, to, vpnServerId: null, externalId: null, CancellationToken.None);

        // Should contain both users
        Assert.Equal(2, res.OverviewUserItems.Count);

        var u1 = res.OverviewUserItems.Single(x => x.ExternalId == "u1");
        Assert.Equal("User One", u1.DisplayName);
        Assert.Equal(10, u1.VpnServerId); // single server 10
        Assert.Equal(1, u1.Sessions); // one session id
        Assert.Equal(20, u1.TrafficInBytes);
        Assert.Equal(10, u1.TrafficOutBytes);
        Assert.True(u1.FirstSeen <= u1.LastSeen);

        var u2 = res.OverviewUserItems.Single(x => x.ExternalId == "u2");
        Assert.Equal("User Two", u2.DisplayName);
        Assert.Null(u2.VpnServerId); // multiple servers -> null
        Assert.Equal(2, u2.Sessions); // two distinct sessions
        // traffic deltas: for sB single sample => 0; for sC 8->18 => +10 in, 3->13 => +10 out
        Assert.Equal(10, u2.TrafficInBytes);
        Assert.Equal(10, u2.TrafficOutBytes);
        Assert.True(u2.FirstSeen <= u2.LastSeen);
    }

    [Fact]
    public async Task GetOverviewUsersSeriesFromSessionsAsync_Buckets_ActiveSessions_And_ActiveUsers()
    {
        var (uow, ctx) = CreateUow();
        var baseTs = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var from = baseTs;
        var to = baseTs.AddHours(2);

        var sA = Guid.NewGuid();
        var sB = Guid.NewGuid();

        // u1: one session in bucket 00:00
        ctx.Traffic.Add(
            new VpnServerClientTraffic { Id = 1, VpnServerId = 1, ExternalId = "u1", SessionId = sA, BytesReceived = 10, BytesSent = 5, MeasuredAt = baseTs.AddMinutes(15) });

        // u1 again + u2 in bucket 01:00 (two users, two sessions)
        ctx.Traffic.AddRange(new[]
        {
            new VpnServerClientTraffic { Id = 2, VpnServerId = 1, ExternalId = "u1", SessionId = sA, BytesReceived = 20, BytesSent = 10, MeasuredAt = baseTs.AddHours(1).AddMinutes(10) },
            new VpnServerClientTraffic { Id = 3, VpnServerId = 1, ExternalId = "u2", SessionId = sB, BytesReceived = 7, BytesSent = 3, MeasuredAt = baseTs.AddHours(1).AddMinutes(30) },
        });
        await ctx.SaveChangesAsync();

        var sut = new OpenVpnOverviewSeriesQuery(uow.Object, OverviewQueryTestHelper.CreateTrafficAggregator(uow.Object));
        var res = await sut.GetOverviewUsersSeriesFromSessionsAsync(from, to, OverviewGrouping.Hours, vpnServerId: 1, externalId: null, CancellationToken.None);

        Assert.Equal(3, res.Rows.Count); // 00:00, 01:00, 02:00 (zero-filled)

        var b0 = res.Rows[0];
        Assert.Equal(baseTs, b0.Ts);
        Assert.Equal(1, b0.ActiveSessions);
        Assert.Equal(1, b0.ActiveUsers);

        var b1 = res.Rows[1];
        Assert.Equal(baseTs.AddHours(1), b1.Ts);
        Assert.Equal(2, b1.ActiveSessions);
        Assert.Equal(2, b1.ActiveUsers);

        var b2 = res.Rows[2];
        Assert.Equal(baseTs.AddHours(2), b2.Ts);
        Assert.Equal(0, b2.ActiveSessions);
        Assert.Equal(0, b2.ActiveUsers);

        Assert.Equal(2, res.Summary.PeakActiveSessions);
        Assert.Equal(2, res.Summary.PeakActiveUsers);
    }
}
