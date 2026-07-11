using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;

namespace DataGateMonitor.Tests.DataBase.Services.Query.UserTable;

public class UserQueryServiceExtendedTests
{
    private static DbContextOptions<TestDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static (UserQueryService sut, TestDbContext ctx) CreateSut()
    {
        var ctx = new TestDbContext(CreateOptions());
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.GetQuery<User>()).Returns(new TestQuery<User>(ctx.Users));
        uow.Setup(u => u.GetQuery<UserIdentityLink>()).Returns(new TestQuery<UserIdentityLink>(ctx.UserIdentityLinks));

        var sut = new UserQueryService(
            new EfQueryService<User, int>(uow.Object),
            new EfQueryService<UserIdentityLink, int>(uow.Object),
            uow.Object);
        return (sut, ctx);
    }

    [Fact]
    public async Task GetAll_ReturnsAllUsers()
    {
        var (sut, ctx) = CreateSut();
        await ctx.Users.AddRangeAsync(
            new User { Id = 1, DisplayName = "a" },
            new User { Id = 2, DisplayName = "b" });
        await ctx.SaveChangesAsync();

        var users = await sut.GetAll(CancellationToken.None);

        Assert.Equal(2, users.Count);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetById_ReturnsUser()
    {
        var (sut, ctx) = CreateSut();
        await ctx.Users.AddAsync(new User { Id = 5, DisplayName = "found" });
        await ctx.SaveChangesAsync();

        var user = await sut.GetById(5, CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("found", user!.DisplayName);
        await ctx.DisposeAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByEmail_ReturnsNull_WhenEmailBlank(string? email)
    {
        var (sut, ctx) = CreateSut();

        var user = await sut.GetByEmail(email!, CancellationToken.None);

        Assert.Null(user);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByEmail_MatchesCaseInsensitively()
    {
        var (sut, ctx) = CreateSut();
        await ctx.Users.AddAsync(new User { Id = 1, DisplayName = "u", Email = "User@Example.COM" });
        await ctx.SaveChangesAsync();

        var user = await sut.GetByEmail("  user@example.com  ", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal(1, user!.Id);
        await ctx.DisposeAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnyByEmail_ReturnsFalse_WhenEmailBlank(string? email)
    {
        var (sut, ctx) = CreateSut();

        var exists = await sut.AnyByEmail(email!, CancellationToken.None);

        Assert.False(exists);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task AnyByEmail_ReturnsTrue_WhenMatchExists()
    {
        var (sut, ctx) = CreateSut();
        await ctx.Users.AddAsync(new User { Id = 1, DisplayName = "u", Email = "exists@test.com" });
        await ctx.SaveChangesAsync();

        var exists = await sut.AnyByEmail("EXISTS@test.com", CancellationToken.None);

        Assert.True(exists);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task Search_FiltersByPredicate()
    {
        var (sut, ctx) = CreateSut();
        await ctx.Users.AddRangeAsync(
            new User { Id = 1, DisplayName = "blocked", IsBlocked = true },
            new User { Id = 2, DisplayName = "active", IsBlocked = false });
        await ctx.SaveChangesAsync();

        var blocked = await sut.Search(u => u.IsBlocked, CancellationToken.None);

        Assert.Single(blocked);
        Assert.Equal(1, blocked[0].Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetUsersWithNonEmptyEmailAsync_ReturnsOnlyUsersWithEmail()
    {
        var (sut, ctx) = CreateSut();
        await ctx.Users.AddRangeAsync(
            new User { Id = 1, DisplayName = "no-email", Email = null },
            new User { Id = 2, DisplayName = "empty", Email = "" },
            new User { Id = 3, DisplayName = "has", Email = "a@b.com" });
        await ctx.SaveChangesAsync();

        var users = await sut.GetUsersWithNonEmptyEmailAsync(CancellationToken.None);

        Assert.Single(users);
        Assert.Equal(3, users[0].Id);
        await ctx.DisposeAsync();
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<UserIdentityLink> UserIdentityLinks => Set<UserIdentityLink>();
    }
}
