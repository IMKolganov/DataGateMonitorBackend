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
