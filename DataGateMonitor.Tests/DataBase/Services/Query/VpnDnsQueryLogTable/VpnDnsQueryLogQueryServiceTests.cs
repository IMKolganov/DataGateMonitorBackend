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
