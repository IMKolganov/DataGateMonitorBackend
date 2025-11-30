using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerStatusLogTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.OpenVpnServerStatusLogTable;

public class OpenVpnServerStatusLogQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<OpenVpnServerStatusLog> OpenVpnServerStatusLogs => Set<OpenVpnServerStatusLog>();
    }

    private static List<OpenVpnServerStatusLog> CreateSample()
    {
        var sessionA = Guid.NewGuid();
        var sessionB = Guid.NewGuid();
        return new List<OpenVpnServerStatusLog>
        {
            new() { Id = 1, VpnServerId = 10, SessionId = sessionA, ServerLocalIp = "10.0.0.1", Version = "2.5" },
            new() { Id = 2, VpnServerId = 20, SessionId = sessionA, ServerLocalIp = "10.0.0.2", Version = "2.5" },
            new() { Id = 3, VpnServerId = 10, SessionId = sessionB, ServerLocalIp = "10.0.0.3", Version = "2.6" },
            new() { Id = 4, VpnServerId = 30, SessionId = sessionB, ServerLocalIp = "10.0.0.4", Version = "2.6" },
        };
    }

    private static (Mock<IQueryService<OpenVpnServerStatusLog, int>> q, TestDbContext ctx, List<OpenVpnServerStatusLog> data) CreateEfBackedQuery()
    {
        var data = CreateSample();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.OpenVpnServerStatusLogs.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<OpenVpnServerStatusLog, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<OpenVpnServerStatusLog, object>>[]>()))
            .Returns(ctx.OpenVpnServerStatusLogs);
        return (mock, ctx, data);
    }

    [Fact]
    public async Task GetAllAsync_Delegates_To_IQueryService()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        q.Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(data)
         .Verifiable();

        var sut = new OpenVpnServerStatusLogQueryService(q.Object);
        var result = await sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllByVpnServerId_Returns_Filtered_List()
    {
        var (q, ctx, _) = CreateEfBackedQuery();
        // delegate WhereAsync to real filtered result using setup
        q.Setup(x => x.WhereAsync(It.IsAny<Expression<Func<OpenVpnServerStatusLog, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerStatusLog, object>>[]>()))
         .Returns<Expression<Func<OpenVpnServerStatusLog, bool>>, Func<IQueryable<OpenVpnServerStatusLog>, IOrderedQueryable<OpenVpnServerStatusLog>>?, bool, CancellationToken, Expression<Func<OpenVpnServerStatusLog, object>>[]>((pred, _, _, _, _) =>
             Task.FromResult(ctx.OpenVpnServerStatusLogs.Where(pred).ToList()))
         .Verifiable();

        var sut = new OpenVpnServerStatusLogQueryService(q.Object);
        var result = await sut.GetAllByVpnServerId(10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.All(result, r => Assert.Equal(10, r.VpnServerId));
        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetBySessionIdAndVpnServerIdAsync_Uses_Query_AsNoTracking()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        var target = data.First();
        var sut = new OpenVpnServerStatusLogQueryService(q.Object);

        var result = await sut.GetBySessionIdAndVpnServerIdAsync(target.SessionId, target.VpnServerId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var (q, ctx, _) = CreateEfBackedQuery();
        var expected = new OpenVpnServerStatusLog { Id = 123, VpnServerId = 999, SessionId = Guid.NewGuid() };
        q.Setup(x => x.FindByIdAsync(123, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerStatusLog, object>>[]>()))
         .ReturnsAsync(expected)
         .Verifiable();

        var sut = new OpenVpnServerStatusLogQueryService(q.Object);
        var result = await sut.GetByIdAsync(123, CancellationToken.None);

        Assert.Same(expected, result);
        q.Verify(x => x.FindByIdAsync(123, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerStatusLog, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAndVpnServerIdAsync_Uses_Query_AsNoTracking()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        var target = data.First(x => x.VpnServerId == 10);
        var sut = new OpenVpnServerStatusLogQueryService(q.Object);

        var result = await sut.GetByIdAndVpnServerIdAsync(target.Id, target.VpnServerId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
        Assert.Equal(target.VpnServerId, result!.VpnServerId);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var (q, ctx, data) = CreateEfBackedQuery();

        var paged = new TestPagedResult<OpenVpnServerStatusLog>
        {
            Page = 2,
            PageSize = 2,
            TotalCount = data.Count,
            Items = data.Skip(1).Take(2).ToList()
        };

        q.Setup(x => x.PageAsync(2, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerStatusLog, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<OpenVpnServerStatusLog>)
         .Verifiable();

        var sut = new OpenVpnServerStatusLogQueryService(q.Object);
        var result = await sut.GetPageAsync(2, 2, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.PageAsync(2, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerStatusLog, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
