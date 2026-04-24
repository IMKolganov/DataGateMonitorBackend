using System.Linq.Expressions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTokenTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.IssuedOvpnFileTokenTable;

public class IssuedOvpnFileTokenQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<IssuedOvpnFileToken> IssuedOvpnFileTokens => Set<IssuedOvpnFileToken>();
    }

    private static List<IssuedOvpnFileToken> CreateSample() => new()
    {
        new IssuedOvpnFileToken { Id = 1, IssuedOvpnFileId = 10, Token = "tok-1", CreatedAt = DateTimeOffset.UtcNow, IsUsed = false },
        new IssuedOvpnFileToken { Id = 2, IssuedOvpnFileId = 10, Token = "tok-2", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1), IsUsed = true },
        new IssuedOvpnFileToken { Id = 3, IssuedOvpnFileId = 20, Token = "tok-3", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2), IsUsed = false },
        new IssuedOvpnFileToken { Id = 4, IssuedOvpnFileId = 30, Token = "tok-4", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-3), IsUsed = false },
    };

    private static (Mock<IQueryService<IssuedOvpnFileToken, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<IssuedOvpnFileToken> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.IssuedOvpnFileTokens.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<IssuedOvpnFileToken, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>()))
            .Returns(ctx.IssuedOvpnFileTokens);
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

        var sut = new IssuedOvpnFileTokenQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var target = new IssuedOvpnFileToken { Id = 42, IssuedOvpnFileId = 77, Token = "tok-x", CreatedAt = DateTimeOffset.UtcNow };
        var (q, ctx) = CreateEfBackedQuery(new[] { target });
        q.Setup(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>()))
         .ReturnsAsync(target)
         .Verifiable();

        var sut = new IssuedOvpnFileTokenQueryService(q.Object);
        var result = await sut.GetById(42, CancellationToken.None);

        Assert.Same(target, result);
        q.Verify(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        var paged = new TestPagedResult<IssuedOvpnFileToken>
        {
            Page = 2,
            PageSize = 10,
            TotalCount = data.Count,
            Items = data.Skip(1).Take(2).ToList()
        };

        q.Setup(x => x.Page(2, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<IssuedOvpnFileToken>)
         .Verifiable();

        var sut = new IssuedOvpnFileTokenQueryService(q.Object);
        var result = await sut.GetPage(2, 10, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.Page(2, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIssuedFileIdsAsync_Returns_Empty_And_DoesNot_Query_When_Input_Null()
    {
        var q = new Mock<IQueryService<IssuedOvpnFileToken, int>>(MockBehavior.Strict);
        // Ensure Query is never called
        q.Setup(x => x.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>())).Verifiable();

        var sut = new IssuedOvpnFileTokenQueryService(q.Object);
        var result = await sut.GetByIssuedFileIds(null!, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
        q.Verify(x => x.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>()), Times.Never);
    }

    [Fact]
    public async Task GetByIssuedFileIdsAsync_Returns_Empty_And_DoesNot_Query_When_Input_Empty()
    {
        var q = new Mock<IQueryService<IssuedOvpnFileToken, int>>(MockBehavior.Strict);
        q.Setup(x => x.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>())).Verifiable();

        var sut = new IssuedOvpnFileTokenQueryService(q.Object);
        var result = await sut.GetByIssuedFileIds(Array.Empty<int>(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
        q.Verify(x => x.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<IssuedOvpnFileToken, object>>[]>()), Times.Never);
    }

    [Fact]
    public async Task GetByIssuedFileIdsAsync_Filters_By_Distinct_FileIds()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileTokenQueryService(q.Object);

        var result = await sut.GetByIssuedFileIds(new[] { 10, 10, 30, 999 }, CancellationToken.None);

        // Expect tokens with IssuedOvpnFileId == 10 or 30
        Assert.True(result.All(x => x.IssuedOvpnFileId == 10 || x.IssuedOvpnFileId == 30));
        var expectedIds = ctx.IssuedOvpnFileTokens
            .Where(x => x.IssuedOvpnFileId == 10 || x.IssuedOvpnFileId == 30)
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        var resultIds = result.Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal(expectedIds, resultIds);

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByTokenAsync_Returns_Entity_By_Token()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileTokenQueryService(q.Object);

        var result = await sut.GetByToken("tok-3", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("tok-3", result!.Token);
        Assert.Equal(20, result.IssuedOvpnFileId);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetRequiredByTokenAsync_Returns_Entity_When_Found()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileTokenQueryService(q.Object);

        var result = await sut.GetRequiredByToken("tok-4", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(30, result.IssuedOvpnFileId);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetRequiredByTokenAsync_Throws_When_Not_Found()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileTokenQueryService(q.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.GetRequiredByToken("missing-token", CancellationToken.None));

        Assert.Contains("missing-token", ex.Message);
        await ctx.DisposeAsync();
    }
}
