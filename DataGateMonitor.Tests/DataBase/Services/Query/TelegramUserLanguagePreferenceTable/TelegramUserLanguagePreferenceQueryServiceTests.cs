using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;

public class TelegramUserLanguagePreferenceQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TelegramUserLanguagePreference> TelegramUserLanguagePreferences => Set<TelegramUserLanguagePreference>();
    }

    private static List<TelegramUserLanguagePreference> CreateSample() => new()
    {
        new TelegramUserLanguagePreference { Id = 1, TelegramId = 111, PreferredLanguage = Language.English },
        new TelegramUserLanguagePreference { Id = 2, TelegramId = 222, PreferredLanguage = Language.Russian },
        new TelegramUserLanguagePreference { Id = 3, TelegramId = 333, PreferredLanguage = Language.Greek },
    };

    private static (Mock<IQueryService<TelegramUserLanguagePreference, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<TelegramUserLanguagePreference> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.TelegramUserLanguagePreferences.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<TelegramUserLanguagePreference, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<TelegramUserLanguagePreference, object>>[]>()))
            .Returns(ctx.TelegramUserLanguagePreferences);
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

        var sut = new TelegramUserLanguagePreferenceQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var target = new TelegramUserLanguagePreference { Id = 42, TelegramId = 999, PreferredLanguage = Language.Greek };
        var (q, ctx) = CreateEfBackedQuery(new[] { target });
        q.Setup(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<TelegramUserLanguagePreference, object>>[]>()))
         .ReturnsAsync(target)
         .Verifiable();

        var sut = new TelegramUserLanguagePreferenceQueryService(q.Object);
        var result = await sut.GetById(42, CancellationToken.None);

        Assert.Same(target, result);
        q.Verify(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<TelegramUserLanguagePreference, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByTelegramId_Filters_Via_Query()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new TelegramUserLanguagePreferenceQueryService(q.Object);

        var result = await sut.GetByTelegramId(222, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(222, result!.TelegramId);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task AnyByTelegramId_Delegates_To_AnyAsync()
    {
        var (q, ctx) = CreateEfBackedQuery(Array.Empty<TelegramUserLanguagePreference>());
        q.Setup(x => x.Any(It.IsAny<Expression<Func<TelegramUserLanguagePreference, bool>>>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(true)
         .Verifiable();

        var sut = new TelegramUserLanguagePreferenceQueryService(q.Object);
        var found = await sut.AnyByTelegramId(555, CancellationToken.None);

        Assert.True(found);
        q.Verify(x => x.Any(It.IsAny<Expression<Func<TelegramUserLanguagePreference, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        var paged = new TestPagedResult<TelegramUserLanguagePreference>
        {
            Page = 3,
            PageSize = 1,
            TotalCount = data.Count,
            Items = data.Skip(2).Take(1).ToList()
        };

        q.Setup(x => x.Page(3, 1, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<TelegramUserLanguagePreference, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<TelegramUserLanguagePreference>)
         .Verifiable();

        var sut = new TelegramUserLanguagePreferenceQueryService(q.Object);
        var result = await sut.GetPage(3, 1, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.Page(3, 1, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<TelegramUserLanguagePreference, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
