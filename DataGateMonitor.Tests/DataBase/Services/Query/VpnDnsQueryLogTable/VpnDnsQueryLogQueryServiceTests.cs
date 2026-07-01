using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.VpnDnsQueryLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;
using DataGateMonitor.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnDnsQueryLogTable;

public class VpnDnsQueryLogQueryServiceTests
{
    [Fact]
    public async Task SearchAsync_FiltersByDomainAndExternalId()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = CreateContext();
        context.VpnDnsQueryLogs.AddRange(
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 1,
                ExternalId = "ext-a",
                ClientIp = "10.51.30.1",
                Domain = "netflix.com",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 2,
                ExternalId = "ext-b",
                ClientIp = "10.51.30.2",
                Domain = "google.com",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            });
        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnDnsQueryLog)] = new TestQuery<VpnDnsQueryLog>(context.VpnDnsQueryLogs)
        };
        var sut = new VpnDnsQueryLogQueryService(new EfQueryService<VpnDnsQueryLog, int>(new TestUnitOfWork(queries)));

        var page = await sut.SearchAsync(new GetVpnDnsQueryRequest
        {
            VpnServerId = 1,
            ExternalId = "ext-a",
            DomainContains = "netflix"
        }, CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("netflix.com", page.Items[0].Domain);
    }

    [Fact]
    public async Task SearchAsync_WithProfileCommonNames_MatchesExternalIdOrCommonName()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = CreateContext();
        context.VpnDnsQueryLogs.AddRange(
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 1,
                ExternalId = "ext-a",
                CommonName = "cn-a",
                ClientIp = "10.51.30.1",
                Domain = "mapped.example",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 2,
                ExternalId = null,
                CommonName = "cn-b",
                ClientIp = "10.51.30.2",
                Domain = "unmapped.example",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 3,
                ExternalId = "ext-other",
                CommonName = "cn-other",
                ClientIp = "10.51.30.3",
                Domain = "other.example",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            });
        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnDnsQueryLog)] = new TestQuery<VpnDnsQueryLog>(context.VpnDnsQueryLogs)
        };
        var sut = new VpnDnsQueryLogQueryService(new EfQueryService<VpnDnsQueryLog, int>(new TestUnitOfWork(queries)));

        var page = await sut.SearchAsync(new GetVpnDnsQueryRequest
        {
            ExternalId = "ext-a",
            VpnServerId = 1
        }, CancellationToken.None, ["cn-b"]);

        Assert.Equal(2, page.TotalCount);
        Assert.Contains(page.Items, x => x.Domain == "mapped.example");
        Assert.Contains(page.Items, x => x.Domain == "unmapped.example");
    }

    [Fact]
    public async Task GetProfileSummaryAsync_GroupsByCommonName()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = CreateContext();
        context.VpnDnsQueryLogs.AddRange(
            new VpnDnsQueryLog
            {
                VpnServerId = 75,
                PiHoleQueryId = 1,
                ExternalId = "ext-a",
                CommonName = "cn-a",
                ClientIp = "10.51.15.1",
                Domain = "one.example",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 75,
                PiHoleQueryId = 2,
                ExternalId = "ext-a",
                CommonName = "cn-a",
                ClientIp = "10.51.15.1",
                Domain = "two.example",
                Status = "FORWARDED",
                QueriedAtUtc = now.AddMinutes(1),
                CreateDate = now,
                LastUpdate = now
            });
        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnDnsQueryLog)] = new TestQuery<VpnDnsQueryLog>(context.VpnDnsQueryLogs)
        };
        var sut = new VpnDnsQueryLogQueryService(new EfQueryService<VpnDnsQueryLog, int>(new TestUnitOfWork(queries)));

        var rows = await sut.GetProfileSummaryAsync(
            "ext-a",
            ["cn-a"],
            vpnServerId: 75,
            fromUtc: null,
            toUtc: null,
            CancellationToken.None);

        Assert.Single(rows);
        Assert.Equal("cn-a", rows[0].CommonName);
        Assert.Equal(2, rows[0].QueryCount);
    }

    [Fact]
    public async Task SearchAsync_WithoutVpnServerId_ReturnsAcrossServers()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = CreateContext();
        context.VpnDnsQueryLogs.AddRange(
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 1,
                ExternalId = "ext-a",
                ClientIp = "10.51.30.1",
                Domain = "one.example",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 2,
                PiHoleQueryId = 2,
                ExternalId = "ext-a",
                ClientIp = "10.51.30.2",
                Domain = "two.example",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            });
        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnDnsQueryLog)] = new TestQuery<VpnDnsQueryLog>(context.VpnDnsQueryLogs)
        };
        var sut = new VpnDnsQueryLogQueryService(new EfQueryService<VpnDnsQueryLog, int>(new TestUnitOfWork(queries)));

        var page = await sut.SearchAsync(new GetVpnDnsQueryRequest
        {
            VpnServerId = 0,
            ExternalId = "ext-a"
        }, CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetServerSummaryAsync_ReturnsZero_WhenServerIdInvalid()
    {
        await using var context = CreateContext();
        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnDnsQueryLog)] = new TestQuery<VpnDnsQueryLog>(context.VpnDnsQueryLogs)
        };
        var sut = new VpnDnsQueryLogQueryService(new EfQueryService<VpnDnsQueryLog, int>(new TestUnitOfWork(queries)));

        var (count, lastAt) = await sut.GetServerSummaryAsync(0, CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Null(lastAt);
    }

    [Fact]
    public async Task GetServerSummaryAsync_ReturnsCountAndLatestTimestamp()
    {
        var older = DateTimeOffset.UtcNow.AddHours(-2);
        var newer = DateTimeOffset.UtcNow.AddMinutes(-5);
        await using var context = CreateContext();
        context.VpnDnsQueryLogs.AddRange(
            new VpnDnsQueryLog
            {
                VpnServerId = 7,
                PiHoleQueryId = 1,
                Domain = "a.example",
                ClientIp = "10.0.0.1",
                Status = "FORWARDED",
                QueriedAtUtc = older,
                CreateDate = older,
                LastUpdate = older
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 7,
                PiHoleQueryId = 2,
                Domain = "b.example",
                ClientIp = "10.0.0.2",
                Status = "FORWARDED",
                QueriedAtUtc = newer,
                CreateDate = newer,
                LastUpdate = newer
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 8,
                PiHoleQueryId = 3,
                Domain = "other.example",
                ClientIp = "10.0.0.3",
                Status = "FORWARDED",
                QueriedAtUtc = newer,
                CreateDate = newer,
                LastUpdate = newer
            });
        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnDnsQueryLog)] = new TestQuery<VpnDnsQueryLog>(context.VpnDnsQueryLogs)
        };
        var sut = new VpnDnsQueryLogQueryService(new EfQueryService<VpnDnsQueryLog, int>(new TestUnitOfWork(queries)));

        var (count, lastAt) = await sut.GetServerSummaryAsync(7, CancellationToken.None);

        Assert.Equal(2, count);
        Assert.NotNull(lastAt);
        Assert.Equal(newer, lastAt);
    }

    [Fact]
    public async Task GetTopDomainsAsync_GroupsByDomainAndCountsDistinctUsers()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = CreateContext();
        context.VpnDnsQueryLogs.AddRange(
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 1,
                ExternalId = "user-a",
                ClientIp = "10.51.30.1",
                Domain = "Netflix.com",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 2,
                ExternalId = "user-a",
                ClientIp = "10.51.30.1",
                Domain = "netflix.com",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 3,
                ExternalId = "user-b",
                ClientIp = "10.51.30.2",
                Domain = "netflix.com",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 4,
                ExternalId = "user-c",
                ClientIp = "10.51.30.3",
                Domain = "google.com",
                Status = "FORWARDED",
                QueriedAtUtc = now,
                CreateDate = now,
                LastUpdate = now
            });
        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnDnsQueryLog)] = new TestQuery<VpnDnsQueryLog>(context.VpnDnsQueryLogs)
        };
        var sut = new VpnDnsQueryLogQueryService(new EfQueryService<VpnDnsQueryLog, int>(new TestUnitOfWork(queries)));

        var rows = await sut.GetTopDomainsAsync(new GetVpnDnsTopDomainsRequest
        {
            VpnServerId = 1,
            Limit = 10
        }, CancellationToken.None);

        Assert.Equal(2, rows.Count);
        Assert.Equal("netflix.com", rows[0].Domain, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(2, rows[0].UniqueUsersCount);
        Assert.Equal(3, rows[0].QueryCount);
        Assert.Equal("google.com", rows[1].Domain);
        Assert.Equal(1, rows[1].UniqueUsersCount);
    }

    [Fact]
    public async Task GetTopDomainsAsync_RespectsDateRange()
    {
        var inside = DateTimeOffset.UtcNow;
        var outside = DateTimeOffset.UtcNow.AddDays(-10);
        await using var context = CreateContext();
        context.VpnDnsQueryLogs.AddRange(
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 1,
                ExternalId = "user-a",
                ClientIp = "10.51.30.1",
                Domain = "recent.example",
                Status = "FORWARDED",
                QueriedAtUtc = inside,
                CreateDate = inside,
                LastUpdate = inside
            },
            new VpnDnsQueryLog
            {
                VpnServerId = 1,
                PiHoleQueryId = 2,
                ExternalId = "user-b",
                ClientIp = "10.51.30.2",
                Domain = "old.example",
                Status = "FORWARDED",
                QueriedAtUtc = outside,
                CreateDate = outside,
                LastUpdate = outside
            });
        await context.SaveChangesAsync();

        var queries = new Dictionary<Type, object>
        {
            [typeof(VpnDnsQueryLog)] = new TestQuery<VpnDnsQueryLog>(context.VpnDnsQueryLogs)
        };
        var sut = new VpnDnsQueryLogQueryService(new EfQueryService<VpnDnsQueryLog, int>(new TestUnitOfWork(queries)));

        var rows = await sut.GetTopDomainsAsync(new GetVpnDnsTopDomainsRequest
        {
            FromUtc = inside.AddHours(-1),
            ToUtc = inside.AddHours(1)
        }, CancellationToken.None);

        Assert.Single(rows);
        Assert.Equal("recent.example", rows[0].Domain);
    }

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
