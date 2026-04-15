using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerEventLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerEventLogTable;

public class VpnServerEventLogQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<VpnServerEventLog> VpnServerEventLogs => Set<VpnServerEventLog>();
    }

    private static List<VpnServerEventLog> CreateSample()
    {
        return new List<VpnServerEventLog>
        {
            new() { Id = 1, VpnServerId = 10, EventType = "ClientConnect" },
            new() { Id = 2, VpnServerId = 10, EventType = "ClientConnect" },
            new() { Id = 3, VpnServerId = 20, EventType = "ClientConnect" },
            new() { Id = 4, VpnServerId = 10, EventType = "ClientDisconnect" },
            new() { Id = 5, VpnServerId = 10, EventType = "ClientDisconnect" },
        };
    }

    private static (Mock<IQueryService<VpnServerEventLog, int>> q, TestDbContext ctx, List<VpnServerEventLog> data) CreateEfBackedQuery()
    {
        var data = CreateSample();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.VpnServerEventLogs.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<VpnServerEventLog, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<VpnServerEventLog, object>>[]>()))
            .Returns(ctx.VpnServerEventLogs);
        return (mock, ctx, data);
    }

    [Fact]
    public async Task GetAllAsync_Delegates_To_IQueryService()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(data)
         .Verifiable();

        var sut = new VpnServerEventLogQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var (q, ctx, _) = CreateEfBackedQuery();
        var expected = new VpnServerEventLog { Id = 42, VpnServerId = 99 };
        q.Setup(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerEventLog, object>>[]>()))
         .ReturnsAsync(expected)
         .Verifiable();

        var sut = new VpnServerEventLogQueryService(q.Object);
        var result = await sut.GetById(42, CancellationToken.None);

        Assert.Same(expected, result);
        q.Verify(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerEventLog, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByVpnServerIdAsync_Normalizes_And_Paginates_Descending_By_Id()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        var sut = new VpnServerEventLogQueryService(q.Object);

        // Pass invalid page/pageSize to test normalization to 1 and 10
        var pageResult = await sut.GetByVpnServerId(10, 0, 0, CancellationToken.None);

        Assert.Equal(1, pageResult.Page);
        Assert.Equal(10, pageResult.PageSize);

        // Now request page 1 size 2 to test ordering and paging
        var result = await sut.GetByVpnServerId(10, 1, 2, CancellationToken.None);

        var expectedAll = data.Where(x => x.VpnServerId == 10).OrderByDescending(x => x.Id).ToList();
        Assert.Equal(expectedAll.Count, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        // Expect top two by Id desc for serverId 10 are Id=5 and Id=4
        Assert.Collection(result.Items,
            i => Assert.Equal(5, i.Id),
            i => Assert.Equal(4, i.Id));

        // Next page should contain next two items: Id=2 and Id=1 (only those with serverId 10)
        var result2 = await sut.GetByVpnServerId(10, 2, 2, CancellationToken.None);
        Assert.Collection(result2.Items,
            i => Assert.Equal(2, i.Id),
            i => Assert.Equal(1, i.Id));

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var (q, ctx, data) = CreateEfBackedQuery();

        var paged = new PagedResponse<VpnServerEventLog>
        {
            Page = 2,
            PageSize = 2,
            TotalCount = data.Count,
            Items = data.Skip(2).Take(2).ToList()
        } as IPagedResult<VpnServerEventLog>;

        q.Setup(x => x.Page(2, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerEventLog, object>>[]>()))
         .ReturnsAsync(paged)
         .Verifiable();

        var sut = new VpnServerEventLogQueryService(q.Object);
        var result = await sut.GetPage(2, 2, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.Page(2, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<VpnServerEventLog, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
