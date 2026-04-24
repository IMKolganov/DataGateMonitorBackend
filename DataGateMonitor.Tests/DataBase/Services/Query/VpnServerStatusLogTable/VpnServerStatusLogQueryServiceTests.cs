using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerStatusLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerStatusLogTable;

public class VpnServerStatusLogQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<VpnServerStatusLog> VpnServerStatusLogs => Set<VpnServerStatusLog>();
    }

    private static List<VpnServerStatusLog> CreateSample()
    {
        var sessionA = Guid.NewGuid();
        var sessionB = Guid.NewGuid();
        return new List<VpnServerStatusLog>
        {
            new() { Id = 1, VpnServerId = 10, SessionId = sessionA, ServerLocalIp = "10.0.0.1", Version = "2.5" },
            new() { Id = 2, VpnServerId = 20, SessionId = sessionA, ServerLocalIp = "10.0.0.2", Version = "2.5" },
            new() { Id = 3, VpnServerId = 10, SessionId = sessionB, ServerLocalIp = "10.0.0.3", Version = "2.6" },
            new() { Id = 4, VpnServerId = 30, SessionId = sessionB, ServerLocalIp = "10.0.0.4", Version = "2.6" },
        };
    }

    private static (Mock<IQueryService<VpnServerStatusLog, int>> q, TestDbContext ctx, List<VpnServerStatusLog> data) CreateEfBackedQuery()
    {
        var data = CreateSample();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.VpnServerStatusLogs.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<VpnServerStatusLog, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<VpnServerStatusLog, object>>[]>()))
            .Returns(ctx.VpnServerStatusLogs);
        return (mock, ctx, data);
    }

    [Fact]
    public async Task GetAllAsync_Delegates_To_IQueryService()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(data)
         .Verifiable();

        var sut = new VpnServerStatusLogQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllByVpnServerId_Returns_Filtered_List()
    {
        var (q, ctx, _) = CreateEfBackedQuery();
        // delegate WhereAsync to real filtered result using setup
        q.Setup(x => x.Where(It.IsAny<Expression<Func<VpnServerStatusLog, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerStatusLog, object>>[]>()))
         .Returns<Expression<Func<VpnServerStatusLog, bool>>, Func<IQueryable<VpnServerStatusLog>, IOrderedQueryable<VpnServerStatusLog>>?, bool, CancellationToken, Expression<Func<VpnServerStatusLog, object>>[]>((pred, _, _, _, _) =>
             Task.FromResult(ctx.VpnServerStatusLogs.Where(pred).ToList()))
         .Verifiable();

        var sut = new VpnServerStatusLogQueryService(q.Object);
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
        var sut = new VpnServerStatusLogQueryService(q.Object);

        var result = await sut.GetBySessionIdAndVpnServerId(target.SessionId, target.VpnServerId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var (q, ctx, _) = CreateEfBackedQuery();
        var expected = new VpnServerStatusLog { Id = 123, VpnServerId = 999, SessionId = Guid.NewGuid() };
        q.Setup(x => x.FindById(123, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerStatusLog, object>>[]>()))
         .ReturnsAsync(expected)
         .Verifiable();

        var sut = new VpnServerStatusLogQueryService(q.Object);
        var result = await sut.GetById(123, CancellationToken.None);

        Assert.Same(expected, result);
        q.Verify(x => x.FindById(123, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerStatusLog, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAndVpnServerIdAsync_Uses_Query_AsNoTracking()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        var target = data.First(x => x.VpnServerId == 10);
        var sut = new VpnServerStatusLogQueryService(q.Object);

        var result = await sut.GetByIdAndVpnServerId(target.Id, target.VpnServerId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
        Assert.Equal(target.VpnServerId, result!.VpnServerId);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var (q, ctx, data) = CreateEfBackedQuery();

        var paged = new TestPagedResult<VpnServerStatusLog>
        {
            Page = 2,
            PageSize = 2,
            TotalCount = data.Count,
            Items = data.Skip(1).Take(2).ToList()
        };

        q.Setup(x => x.Page(2, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerStatusLog, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<VpnServerStatusLog>)
         .Verifiable();

        var sut = new VpnServerStatusLogQueryService(q.Object);
        var result = await sut.GetPage(2, 2, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.Page(2, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerStatusLog, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
