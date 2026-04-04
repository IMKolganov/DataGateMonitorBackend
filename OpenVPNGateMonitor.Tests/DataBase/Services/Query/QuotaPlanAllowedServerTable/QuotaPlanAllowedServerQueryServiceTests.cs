using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;
using Xunit;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.QuotaPlanAllowedServerTable;

public class QuotaPlanAllowedServerQueryServiceTests
{
    [Fact]
    public async Task GetPage_WithNoFilter_CallsPageWithNullPredicate()
    {
        var paged = new TestPagedResult<QuotaPlanAllowedServer>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            Items = []
        };

        var q = new Mock<IQueryService<QuotaPlanAllowedServer, int>>();
        q.Setup(x => x.Page(1, 20, null, It.IsAny<Func<IQueryable<QuotaPlanAllowedServer>, IOrderedQueryable<QuotaPlanAllowedServer>>>(), true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlanAllowedServer, object>>[]>()))
            .ReturnsAsync(paged)
            .Verifiable();

        var sut = new QuotaPlanAllowedServerQueryService(q.Object);
        var result = await sut.GetPage(1, 20, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        q.Verify();
    }

    [Fact]
    public async Task GetListByQuotaPlanId_DelegatesToWhere()
    {
        var list = new List<QuotaPlanAllowedServer>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }
        };

        var q = new Mock<IQueryService<QuotaPlanAllowedServer, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlanAllowedServer, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list)
            .Verifiable();

        var sut = new QuotaPlanAllowedServerQueryService(q.Object);
        var result = await sut.GetListByQuotaPlanId(10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(10, result[0].QuotaPlanId);
        q.Verify();
    }

    [Fact]
    public async Task GetListByVpnServerId_DelegatesToWhere()
    {
        var list = new List<QuotaPlanAllowedServer>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }
        };

        var q = new Mock<IQueryService<QuotaPlanAllowedServer, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlanAllowedServer, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list)
            .Verifiable();

        var sut = new QuotaPlanAllowedServerQueryService(q.Object);
        var result = await sut.GetListByVpnServerId(5, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(5, result[0].VpnServerId);
        q.Verify();
    }

    [Fact]
    public async Task GetVpnServerIdsByQuotaPlanId_ReturnsDistinctVpnServerIds()
    {
        var now = DateTimeOffset.UtcNow;
        var rows = new List<QuotaPlanAllowedServer>
        {
            new() { Id = 1, QuotaPlanId = 3, VpnServerId = 10, CreateDate = now, LastUpdate = now },
            new() { Id = 2, QuotaPlanId = 3, VpnServerId = 10, CreateDate = now, LastUpdate = now },
            new() { Id = 3, QuotaPlanId = 3, VpnServerId = 20, CreateDate = now, LastUpdate = now }
        };

        var q = new Mock<IQueryService<QuotaPlanAllowedServer, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlanAllowedServer, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new QuotaPlanAllowedServerQueryService(q.Object);
        var set = await sut.GetVpnServerIdsByQuotaPlanId(3, CancellationToken.None);

        Assert.Equal(2, set.Count);
        Assert.Contains(10, set);
        Assert.Contains(20, set);
    }

    [Fact]
    public async Task GetDistinctVpnServerIds_UsesQueryAndDistinct()
    {
        var options = new DbContextOptionsBuilder<TestQuotaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new TestQuotaDbContext(options);
        var now = DateTimeOffset.UtcNow;
        ctx.QuotaPlanAllowedServers.AddRange(
            new QuotaPlanAllowedServer { Id = 1, QuotaPlanId = 1, VpnServerId = 100, CreateDate = now, LastUpdate = now },
            new QuotaPlanAllowedServer { Id = 2, QuotaPlanId = 1, VpnServerId = 100, CreateDate = now, LastUpdate = now },
            new QuotaPlanAllowedServer { Id = 3, QuotaPlanId = 2, VpnServerId = 200, CreateDate = now, LastUpdate = now });
        await ctx.SaveChangesAsync();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.GetQuery<QuotaPlanAllowedServer>())
            .Returns(new TestQueryAdapter(ctx.QuotaPlanAllowedServers));

        var ef = new EfQueryService<QuotaPlanAllowedServer, int>(uowMock.Object);
        var sut = new QuotaPlanAllowedServerQueryService(ef);

        var set = await sut.GetDistinctVpnServerIds(CancellationToken.None);

        Assert.Equal(2, set.Count);
        Assert.Contains(100, set);
        Assert.Contains(200, set);
    }

    private sealed class TestQuotaDbContext : DbContext
    {
        public TestQuotaDbContext(DbContextOptions<TestQuotaDbContext> options) : base(options) { }

        public DbSet<QuotaPlanAllowedServer> QuotaPlanAllowedServers => Set<QuotaPlanAllowedServer>();
    }

    private sealed class TestQueryAdapter : IQuery<QuotaPlanAllowedServer>
    {
        private readonly IQueryable<QuotaPlanAllowedServer> _query;

        public TestQueryAdapter(IQueryable<QuotaPlanAllowedServer> query) => _query = query;

        public IQueryable<QuotaPlanAllowedServer> AsQueryable() => _query;
    }
}
