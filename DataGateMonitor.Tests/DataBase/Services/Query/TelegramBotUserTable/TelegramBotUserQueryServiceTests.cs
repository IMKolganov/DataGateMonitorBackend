using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;

namespace DataGateMonitor.Tests.DataBase.Services.Query.TelegramBotUserTable;

public class TelegramBotUserQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TelegramBotUser> TelegramBotUsers => Set<TelegramBotUser>();
    }

    private static List<TelegramBotUser> CreateSample()
        => new()
        {
            new TelegramBotUser { Id = 1, TelegramId = 100, Username = "alice", IsAdmin = true, IsBlocked = false },
            new TelegramBotUser { Id = 2, TelegramId = 200, Username = "bob", IsAdmin = false, IsBlocked = false },
            new TelegramBotUser { Id = 3, TelegramId = 300, Username = "carol", IsAdmin = false, IsBlocked = true },
        };

    private static (Mock<IQueryService<TelegramBotUser, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<TelegramBotUser> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.TelegramBotUsers.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<TelegramBotUser, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<TelegramBotUser, object>>[]>()))
            .Returns(ctx.TelegramBotUsers);
        return (mock, ctx);
    }

    [Fact]
    public async Task GetFiltered_Filters_By_TelegramId()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new TelegramBotUserQueryService(q.Object);

        var result = await sut.GetFiltered(new GetAllTelegramBotUsersRequest { TelegramId = 200 }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetFiltered_Filters_By_IsAdmin()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new TelegramBotUserQueryService(q.Object);

        var result = await sut.GetFiltered(new GetAllTelegramBotUsersRequest { IsAdmin = true }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetFiltered_Filters_By_IsBlocked()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new TelegramBotUserQueryService(q.Object);

        var result = await sut.GetFiltered(new GetAllTelegramBotUsersRequest { IsBlocked = true }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
        await ctx.DisposeAsync();
    }
}
