using System.Linq.Expressions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.IncomingMessageLogTable;

public class IncomingMessageLogQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<IncomingMessageLog> IncomingMessageLogs => Set<IncomingMessageLog>();
    }

    private static List<IncomingMessageLog> CreateSample()
        => new()
        {
            new IncomingMessageLog { Id = 1, TelegramId = 1001, MessageText = "a" },
            new IncomingMessageLog { Id = 2, TelegramId = 1002, MessageText = "b" },
            new IncomingMessageLog { Id = 3, TelegramId = 1001, MessageText = "c" },
            new IncomingMessageLog { Id = 4, TelegramId = 1003, MessageText = "d" }
        };

    private static (Mock<IQueryService<IncomingMessageLog, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<IncomingMessageLog> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.IncomingMessageLogs.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<IncomingMessageLog, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<IncomingMessageLog, object>>[]>()))
            .Returns(ctx.IncomingMessageLogs);
        return (mock, ctx);
    }

    [Fact]
    public async Task GetAllAsync_Delegates_To_IQueryService()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(data)
         .Verifiable();

        var sut = new IncomingMessageLogQueryService(q.Object);

        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var target = new IncomingMessageLog { Id = 42, TelegramId = 999, MessageText = "hello" };
        var (q, ctx) = CreateEfBackedQuery(new[] { target });
        q.Setup(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IncomingMessageLog, object>>[]>()))
         .ReturnsAsync(target)
         .Verifiable();

        var sut = new IncomingMessageLogQueryService(q.Object);
        var result = await sut.GetById(42, CancellationToken.None);

        Assert.Same(target, result);
        q.Verify(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IncomingMessageLog, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageByTelegramIdAsync_Calls_PageAsync_With_Correct_Predicate_And_OrderBy()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        // capture arguments
        Expression<Func<IncomingMessageLog, bool>>? capturedPredicate = null;
        Func<IQueryable<IncomingMessageLog>, IOrderedQueryable<IncomingMessageLog>>? capturedOrderBy = null;

        var paged = new TestPagedResult<IncomingMessageLog>
        {
            Page = 1,
            PageSize = 2,
            TotalCount = 2,
            Items = data.Where(x => x.TelegramId == 1001).OrderByDescending(x => x.Id).Take(2).ToList()
        };

        q.Setup(x => x.Page(
                1,
                2,
                It.IsAny<Expression<Func<IncomingMessageLog, bool>>>(),
                It.IsAny<Func<IQueryable<IncomingMessageLog>, IOrderedQueryable<IncomingMessageLog>>>(),
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<IncomingMessageLog, object>>[]>()))
         .Callback((int page, int size,
             Expression<Func<IncomingMessageLog, bool>>? predicate,
             Func<IQueryable<IncomingMessageLog>, IOrderedQueryable<IncomingMessageLog>>? orderBy,
             bool _, CancellationToken __, Expression<Func<IncomingMessageLog, object>>[] ___) =>
         {
             capturedPredicate = predicate;
             capturedOrderBy = orderBy;
         })
         .ReturnsAsync(paged as IPagedResult<IncomingMessageLog>)
         .Verifiable();

        var sut = new IncomingMessageLogQueryService(q.Object);
        var result = await sut.GetPageByTelegramId(1001, 1, 2, CancellationToken.None);

        Assert.Same(paged, result);
        Assert.NotNull(capturedPredicate);
        Assert.NotNull(capturedOrderBy);

        // verify predicate filters by telegramId
        var filtered = ctx.IncomingMessageLogs.Where(capturedPredicate!).ToList();
        Assert.All(filtered, x => Assert.Equal(1001, x.TelegramId));

        // verify order by Id DESC
        var ordered = capturedOrderBy!(ctx.IncomingMessageLogs);
        var orderedIds = ordered.Select(x => x.Id).ToList();
        var expectedIds = ctx.IncomingMessageLogs.Select(x => x.Id).OrderByDescending(i => i).ToList();
        Assert.Equal(expectedIds, orderedIds);

        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Returns_Paged_Results_Ordered_By_Id_Desc()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        var sut = new IncomingMessageLogQueryService(q.Object);
        var result = await sut.GetPage(1, 2, CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(4, result.Items[0].Id);
        Assert.Equal(3, result.Items[1].Id);
        await ctx.DisposeAsync();
    }
}
