using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.UserTable;

public class UserQueryServiceTests
{
    private static DbContextOptions<TestDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static (UserQueryService sut, TestDbContext ctx) CreateSutWithContext()
    {
        var ctx = new TestDbContext(CreateOptions());
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.GetQuery<User>())
            .Returns(new TestQuery<User>(ctx.Users));
        uow.Setup(u => u.GetQuery<UserIdentityLink>())
            .Returns(new TestQuery<UserIdentityLink>(ctx.UserIdentityLinks));

        var qUser = new EfQueryService<User, int>(uow.Object);
        var qLink = new EfQueryService<UserIdentityLink, int>(uow.Object);
        var sut = new UserQueryService(qUser, qLink);
        return (sut, ctx);
    }

    [Fact]
    public async Task GetByExternalId_Returns_User_When_Link_Exists()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.Users.AddRangeAsync(
            new User { Id = 1, DisplayName = "u1" },
            new User { Id = 2, DisplayName = "u2" }
        );
        await ctx.UserIdentityLinks.AddRangeAsync(
            new UserIdentityLink { Id = 10, Provider = "tg", ExternalId = "ext-1", UserId = 2 }
        );
        await ctx.SaveChangesAsync();

        var user = await sut.GetByExternalIdAsync("ext-1", CancellationToken.None);
        Assert.NotNull(user);
        Assert.Equal(2, user!.Id);
        Assert.Equal("u2", user.DisplayName);
    }

    [Fact]
    public async Task GetByExternalId_Returns_Null_When_No_Link()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.Users.AddAsync(new User { Id = 1, DisplayName = "u1" });
        await ctx.SaveChangesAsync();

        var user = await sut.GetByExternalIdAsync("missing", CancellationToken.None);
        Assert.Null(user);
    }

    [Fact]
    public async Task Paging_Works()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.Users.AddRangeAsync(Enumerable.Range(1, 15).Select(i => new User { Id = i, DisplayName = $"u{i}" }));
        await ctx.SaveChangesAsync();

        var page = await sut.GetPageAsync(2, 10, CancellationToken.None);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(15, page.TotalCount);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(11, page.Items.First().Id);
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserIdentityLink> UserIdentityLinks => Set<UserIdentityLink>();
    }
}
