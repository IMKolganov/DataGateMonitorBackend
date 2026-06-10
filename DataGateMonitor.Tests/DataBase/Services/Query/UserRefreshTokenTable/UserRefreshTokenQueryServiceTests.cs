using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.UserRefreshTokenTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.UserRefreshTokenTable;

public class UserRefreshTokenQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    }

    private static (UserRefreshTokenQueryService sut, TestDbContext ctx) CreateSutWithContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.GetQuery<UserRefreshToken>())
            .Returns(new TestQuery<UserRefreshToken>(ctx.UserRefreshTokens));
        var sut = new UserRefreshTokenQueryService(new EfQueryService<UserRefreshToken, int>(uow.Object));
        return (sut, ctx);
    }

    [Fact]
    public async Task GetAll_Delegates_To_IQueryService()
    {
        var items = new List<UserRefreshToken> { new() { Id = 1, UserId = 1, TokenHash = "a" } };
        var q = new Mock<IQueryService<UserRefreshToken, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var sut = new UserRefreshTokenQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetById_Delegates_To_FindById()
    {
        var item = new UserRefreshToken { Id = 9, UserId = 1, TokenHash = "hash" };
        var q = new Mock<IQueryService<UserRefreshToken, int>>();
        q.Setup(x => x.FindById(9, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<UserRefreshToken, object>>[]>()))
            .ReturnsAsync(item);

        var sut = new UserRefreshTokenQueryService(q.Object);
        var result = await sut.GetById(9, CancellationToken.None);

        Assert.Same(item, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByTokenHash_Throws_WhenHashBlank(string? hash)
    {
        var sut = new UserRefreshTokenQueryService(new Mock<IQueryService<UserRefreshToken, int>>().Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetByTokenHash(hash!, CancellationToken.None));

        Assert.Contains("Token hash is required", ex.Message);
    }

    [Fact]
    public async Task GetByTokenHash_Filters_By_Hash()
    {
        var now = DateTimeOffset.UtcNow;
        var (sut, ctx) = CreateSutWithContext();
        ctx.UserRefreshTokens.AddRange(
            new UserRefreshToken
            {
                Id = 1, UserId = 1, TokenHash = "wanted",
                CreatedAt = now, ExpiresAt = now.AddDays(1),
                CreateDate = now, LastUpdate = now,
            },
            new UserRefreshToken
            {
                Id = 2, UserId = 1, TokenHash = "other",
                CreatedAt = now, ExpiresAt = now.AddDays(1),
                CreateDate = now, LastUpdate = now,
            });
        await ctx.SaveChangesAsync();

        var result = await sut.GetByTokenHash("wanted", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPage_Delegates_To_Page()
    {
        var paged = new TestPagedResult<UserRefreshToken>
        {
            Page = 1,
            PageSize = 5,
            TotalCount = 1,
            Items = [new UserRefreshToken { Id = 1, UserId = 1, TokenHash = "h" }],
        } as IPagedResult<UserRefreshToken>;

        var q = new Mock<IQueryService<UserRefreshToken, int>>();
        q.Setup(x => x.Page(1, 5, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<UserRefreshToken, object>>[]>()))
            .ReturnsAsync(paged);

        var sut = new UserRefreshTokenQueryService(q.Object);
        var result = await sut.GetPage(1, 5, CancellationToken.None);

        Assert.Same(paged, result);
    }

    [Fact]
    public async Task Search_Delegates_To_Where()
    {
        var rows = new List<UserRefreshToken> { new() { Id = 3, UserId = 5, TokenHash = "h" } };
        var q = new Mock<IQueryService<UserRefreshToken, int>>();
        q.Setup(x => x.Where(It.IsAny<Expression<Func<UserRefreshToken, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new UserRefreshTokenQueryService(q.Object);
        var result = await sut.Search(t => t.UserId == 5, CancellationToken.None);

        Assert.Same(rows, result);
    }
}
