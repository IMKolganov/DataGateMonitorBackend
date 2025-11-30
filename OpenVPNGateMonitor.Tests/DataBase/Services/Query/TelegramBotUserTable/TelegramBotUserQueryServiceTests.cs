using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.TelegramBotUserTable;

public class TelegramBotUserQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TelegramBotUser> TelegramBotUsers => Set<TelegramBotUser>();
    }

    private static (Mock<IQueryService<TelegramBotUser, int>> q, TestDbContext ctx, List<TelegramBotUser> data) CreateEfBackedQuery()
    {
        var data = new List<TelegramBotUser>
        {
            new() { Id = 1, TelegramId = 1001, IsAdmin = true },
            new() { Id = 2, TelegramId = 1002, IsAdmin = false },
            new() { Id = 3, TelegramId = 1003, IsAdmin = true },
        };

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.TelegramBotUsers.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<TelegramBotUser, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<TelegramBotUser, object>>[]>()))
            .Returns(ctx.TelegramBotUsers);
        return (mock, ctx, data);
    }

    [Fact]
    public async Task GetAllAsync_Delegates_To_IQueryService()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        q.Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(data)
         .Verifiable();

        var sut = new TelegramBotUserQueryService(q.Object);
        var result = await sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllAdminsAsync_Returns_Only_Admins()
    {
        var (q, ctx, _) = CreateEfBackedQuery();
        var sut = new TelegramBotUserQueryService(q.Object);

        var result = await sut.GetAllAdminsAsync(CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.All(result, x => Assert.True(x.IsAdmin));
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var (q, ctx, _) = CreateEfBackedQuery();
        var expected = new TelegramBotUser { Id = 42, TelegramId = 123456, IsAdmin = false };
        q.Setup(x => x.FindByIdAsync(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<TelegramBotUser, object>>[]>()))
         .ReturnsAsync(expected)
         .Verifiable();

        var sut = new TelegramBotUserQueryService(q.Object);
        var result = await sut.GetByIdAsync(42, CancellationToken.None);

        Assert.Same(expected, result);
        q.Verify(x => x.FindByIdAsync(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<TelegramBotUser, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task AnyByTelegramIdAsync_Delegates_To_AnyAsync()
    {
        var (q, ctx, _) = CreateEfBackedQuery();
        q.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<TelegramBotUser, bool>>>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(true)
         .Verifiable();

        var sut = new TelegramBotUserQueryService(q.Object);
        var exists = await sut.AnyByTelegramIdAsync(1001, CancellationToken.None);

        Assert.True(exists);
        q.Verify(x => x.AnyAsync(It.IsAny<Expression<Func<TelegramBotUser, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByTelegramIdAsync_Uses_Query_FirstOrDefaultAsync()
    {
        var (q, ctx, data) = CreateEfBackedQuery();
        var target = data.First();
        var sut = new TelegramBotUserQueryService(q.Object);

        var found = await sut.GetByTelegramIdAsync(target.TelegramId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(target.Id, found!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var (q, ctx, data) = CreateEfBackedQuery();

        var paged = new PagedResponse<TelegramBotUser>
        {
            Page = 1,
            PageSize = 2,
            TotalCount = data.Count,
            Items = data.Take(2).ToList()
        } as IPagedResult<TelegramBotUser>;

        q.Setup(x => x.PageAsync(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<TelegramBotUser, object>>[]>()))
         .ReturnsAsync(paged)
         .Verifiable();

        var sut = new TelegramBotUserQueryService(q.Object);
        var result = await sut.GetPageAsync(1, 2, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.PageAsync(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<TelegramBotUser, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
