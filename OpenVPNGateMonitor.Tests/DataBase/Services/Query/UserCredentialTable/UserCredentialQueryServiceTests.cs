using System.Linq.Expressions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.UserCredentialTable;

public class UserCredentialQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    }

    private static (Mock<IQueryService<UserCredential, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<UserCredential> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.UserCredentials.AddRange(data);
        ctx.SaveChanges();

        var q = new Mock<IQueryService<UserCredential, int>>();
        q.Setup(x => x.Query(true, It.IsAny<Expression<Func<UserCredential, object>>[]>()))
         .Returns(ctx.UserCredentials);
        return (q, ctx);
    }

    [Fact]
    public async Task GetAllAsync_Delegates_To_IQueryService()
    {
        var items = new List<UserCredential>
        {
            new() { Id = 1, NormalizedLogin = "USER1", UserId = 100 },
            new() { Id = 2, NormalizedLogin = "USER2", UserId = 200 }
        };

        var q = new Mock<IQueryService<UserCredential, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(items)
         .Verifiable();

        var sut = new UserCredentialQueryService(q.Object);
        var res = await sut.GetAll(CancellationToken.None);

        Assert.Equal(2, res.Count);
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var item = new UserCredential { Id = 77, NormalizedLogin = "U77", UserId = 777 };
        var q = new Mock<IQueryService<UserCredential, int>>();
        q.Setup(x => x.FindById(77, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<UserCredential, object>>[]>()))
         .ReturnsAsync(item)
         .Verifiable();

        var sut = new UserCredentialQueryService(q.Object);
        var res = await sut.GetById(77, CancellationToken.None);
        Assert.Same(item, res);
        q.Verify();
    }

    [Fact]
    public async Task GetByNormalizedLogin_Filters_By_NormalizedLogin()
    {
        var data = new List<UserCredential>
        {
            new() { Id = 1, Login = "a@a", PasswordHash = "h1", NormalizedLogin = "AAA", UserId = 1 },
            new() { Id = 2, Login = "b@b", PasswordHash = "h2", NormalizedLogin = "BBB", UserId = 2 }
        };
        var (q, ctx) = CreateEfBackedQuery(data);

        var sut = new UserCredentialQueryService(q.Object);
        var res = await sut.GetByNormalizedLogin("BBB", CancellationToken.None);
        Assert.NotNull(res);
        Assert.Equal(2, res!.Id);

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByUserId_Filters_By_UserId()
    {
        var data = new List<UserCredential>
        {
            new() { Id = 1, Login = "a@a", PasswordHash = "h1", NormalizedLogin = "AAA", UserId = 1 },
            new() { Id = 2, Login = "b@b", PasswordHash = "h2", NormalizedLogin = "BBB", UserId = 2 }
        };
        var (q, ctx) = CreateEfBackedQuery(data);

        var sut = new UserCredentialQueryService(q.Object);
        var res = await sut.GetByUserId(1, CancellationToken.None);
        Assert.NotNull(res);
        Assert.Equal("AAA", res!.NormalizedLogin);

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task AnyByUserId_Delegates_To_AnyAsync()
    {
        Expression<Func<UserCredential, bool>>? captured = null;
        var q = new Mock<IQueryService<UserCredential, int>>();
        q.Setup(x => x.Any(It.IsAny<Expression<Func<UserCredential, bool>>>(), It.IsAny<CancellationToken>()))
         .Callback((Expression<Func<UserCredential, bool>> predicate, CancellationToken _) => captured = predicate)
         .ReturnsAsync(true)
         .Verifiable();

        var sut = new UserCredentialQueryService(q.Object);
        var res = await sut.AnyByUserId(5, CancellationToken.None);
        Assert.True(res);
        Assert.NotNull(captured);

        // validate predicate logic on sample data
        var sample = new[]
        {
            new UserCredential { Id = 1, UserId = 4 },
            new UserCredential { Id = 2, UserId = 5 }
        }.AsQueryable();
        var filtered = sample.Where(captured!).ToList();
        Assert.Single(filtered);
        Assert.Equal(2, filtered[0].Id);

        q.Verify();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var paged = new Tests.Helpers.TestPagedResult<UserCredential>
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 25,
            Items = new List<UserCredential> { new() { Id = 11, NormalizedLogin = "U11", UserId = 11 } }
        } as IPagedResult<UserCredential>;

        var q = new Mock<IQueryService<UserCredential, int>>();
        q.Setup(x => x.Page(2, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<UserCredential, object>>[]>()))
         .ReturnsAsync(paged)
         .Verifiable();

        var sut = new UserCredentialQueryService(q.Object);
        var res = await sut.GetPage(2, 10, CancellationToken.None);
        Assert.Same(paged, res);
        q.Verify();
    }
}
