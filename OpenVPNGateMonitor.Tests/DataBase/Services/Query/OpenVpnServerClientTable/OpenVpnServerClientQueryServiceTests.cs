using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.OpenVpnServerClientTable;

public class OpenVpnServerClientQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<OpenVpnServerClient> Clients => Set<OpenVpnServerClient>();
    }

    private static (Mock<IQueryService<OpenVpnServerClient, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<OpenVpnServerClient> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.Clients.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<OpenVpnServerClient, int>>();
        mock
            .Setup(x => x.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<OpenVpnServerClient, object>>[]>()))
            .Returns(ctx.Clients);
        return (mock, ctx);
    }

    private static List<OpenVpnServerClient> Sample() => new()
    {
        new OpenVpnServerClient { Id = 1, VpnServerId = 10, IsConnected = true,  SessionId = Guid.NewGuid(), ExternalId = "u1", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-3) },
        new OpenVpnServerClient { Id = 2, VpnServerId = 10, IsConnected = false, SessionId = Guid.NewGuid(), ExternalId = "u2", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-2) },
        new OpenVpnServerClient { Id = 3, VpnServerId = 20, IsConnected = true,  SessionId = Guid.NewGuid(), ExternalId = "u1", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-1) },
    };

    [Fact]
    public async Task GetAllAsync_Delegates()
    {
        var data = Sample();
        var (q, ctx) = CreateEfBackedQuery(data);

        q.Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(data)
         .Verifiable();

        var sut = new OpenVpnServerClientQueryService(q.Object);
        var result = await sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(3, result.Count);
        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllConnectedByVpnServerIdAsync_Uses_WhereAsync_With_Filter()
    {
        var data = Sample();
        var (q, ctx) = CreateEfBackedQuery(data);

        Expression<Func<OpenVpnServerClient, bool>>? captured = null;

        q.Setup(x => x.WhereAsync(
                It.IsAny<Expression<Func<OpenVpnServerClient, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<OpenVpnServerClient, object>>[]>()
            ))
         .Callback((Expression<Func<OpenVpnServerClient, bool>> predicate,
             Func<IQueryable<OpenVpnServerClient>, IOrderedQueryable<OpenVpnServerClient>>? orderBy,
             bool _, CancellationToken __, Expression<Func<OpenVpnServerClient, object>>[] ___) =>
         {
             captured = predicate;
         })
         .ReturnsAsync(data.Where(x => x.VpnServerId == 10 && x.IsConnected).ToList())
         .Verifiable();

        var sut = new OpenVpnServerClientQueryService(q.Object);
        var result = await sut.GetAllConnectedByVpnServerIdAsync(10, CancellationToken.None);

        Assert.All(result, x => { Assert.Equal(10, x.VpnServerId); Assert.True(x.IsConnected); });
        Assert.NotNull(captured);
        var filtered = ctx.Clients.Where(captured!).ToList();
        Assert.All(filtered, x => { Assert.Equal(10, x.VpnServerId); Assert.True(x.IsConnected); });
        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates()
    {
        var entity = new OpenVpnServerClient { Id = 42, VpnServerId = 10, IsConnected = true, SessionId = Guid.NewGuid(), ConnectedSince = DateTimeOffset.UtcNow };
        var (q, ctx) = CreateEfBackedQuery(new[] { entity });

        q.Setup(x => x.FindByIdAsync(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerClient, object>>[]>()))
         .ReturnsAsync(entity)
         .Verifiable();

        var sut = new OpenVpnServerClientQueryService(q.Object);
        var res = await sut.GetByIdAsync(42, CancellationToken.None);
        Assert.Same(entity, res);
        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetBySessionAndServerIdAsync_Queries_DbSet()
    {
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        var data = new List<OpenVpnServerClient>
        {
            new() { Id = 1, VpnServerId = 10, SessionId = s1, IsConnected = true, ConnectedSince = DateTimeOffset.UtcNow.AddHours(-2) },
            new() { Id = 2, VpnServerId = 10, SessionId = s2, IsConnected = true, ConnectedSince = DateTimeOffset.UtcNow.AddHours(-1) },
        };

        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new OpenVpnServerClientQueryService(q.Object);

        var found = await sut.GetBySessionAndServerIdAsync(s2, 10, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(2, found!.Id);

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates()
    {
        var data = Sample();
        var (q, ctx) = CreateEfBackedQuery(data);

        var paged = new TestPagedResult<OpenVpnServerClient>
        {
            Page = 1,
            PageSize = 2,
            TotalCount = data.Count,
            Items = data.Take(2).ToList()
        };

        q.Setup(x => x.PageAsync(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerClient, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<OpenVpnServerClient>)
         .Verifiable();

        var sut = new OpenVpnServerClientQueryService(q.Object);
        var res = await sut.GetPageAsync(1, 2, CancellationToken.None);
        Assert.Same(paged, res);
        q.Verify();
        await ctx.DisposeAsync();
    }
}
