using System.Linq.Expressions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.LocalizationTextTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.LocalizationTextTable;

public class LocalizationTextQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<LocalizationText> LocalizationTexts => Set<LocalizationText>();
    }

    private static List<LocalizationText> CreateSample() => new()
    {
        new LocalizationText { Id = 1, Key = "hello", Language = Language.English, Text = "Hello" },
        new LocalizationText { Id = 2, Key = "hello", Language = Language.Russian, Text = "Привет" },
        new LocalizationText { Id = 3, Key = "bye",   Language = Language.English, Text = "Bye" },
        new LocalizationText { Id = 4, Key = "bye",   Language = Language.Russian, Text = "Пока" },
    };

    private static (Mock<IQueryService<LocalizationText, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<LocalizationText> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.LocalizationTexts.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<LocalizationText, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<LocalizationText, object>>[]>()))
            .Returns(ctx.LocalizationTexts);
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

        var sut = new LocalizationTextQueryService(q.Object);

        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var target = new LocalizationText { Id = 42, Key = "k", Language = Language.English, Text = "v" };
        var (q, ctx) = CreateEfBackedQuery(new[] { target });
        q.Setup(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<LocalizationText, object>>[]>()))
         .ReturnsAsync(target)
         .Verifiable();

        var sut = new LocalizationTextQueryService(q.Object);
        var result = await sut.GetById(42, CancellationToken.None);

        Assert.Same(target, result);
        q.Verify(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<LocalizationText, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetTextValueByKeyAndLanguageAsync_Returns_Text_When_Found()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new LocalizationTextQueryService(q.Object);

        var text = await sut.GetTextValueByKeyAndLanguage("hello", Language.Russian, CancellationToken.None);

        Assert.Equal("Привет", text);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetTextValueByKeyAndLanguageAsync_Returns_Null_When_Not_Found()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new LocalizationTextQueryService(q.Object);

        var text = await sut.GetTextValueByKeyAndLanguage("missing", Language.English, CancellationToken.None);

        Assert.Null(text);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        var paged = new TestPagedResult<LocalizationText>
        {
            Page = 1,
            PageSize = 2,
            TotalCount = data.Count,
            Items = data.Take(2).ToList()
        };

        q.Setup(x => x.Page(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<LocalizationText, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<LocalizationText>)
         .Verifiable();

        var sut = new LocalizationTextQueryService(q.Object);
        var result = await sut.GetPage(1, 2, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.Page(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<LocalizationText, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
