using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query;

public class ClientApplicationQueryServiceTests
{
    // helpers
    private static DbContextOptions<TestDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static (ClientApplicationQueryService sut, TestDbContext ctx) CreateSutWithContext()
    {
        var options = CreateOptions();
        var ctx = new TestDbContext(options);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.GetQuery<ClientApplication>())
            .Returns(new TestQuery<ClientApplication>(ctx.ClientApplications));

        var q = new EfQueryService<ClientApplication, int>(uow.Object);
        var sut = new ClientApplicationQueryService(q);
        return (sut, ctx);
    }

    [Fact]
    public async Task GetAll_Is_Not_Revoked()
    {
        var (sut, ctx) = CreateSutWithContext();
        var list = new List<ClientApplication>
        {
            new() { Id = 1, Name = "A", ClientId = "c1", IsRevoked = false },
            new() { Id = 2, Name = "B", ClientId = "c2", IsRevoked = true },
            new() { Id = 3, Name = "C", ClientId = "c3", IsRevoked = false }
        };
        await ctx.ClientApplications.AddRangeAsync(list);
        await ctx.SaveChangesAsync();

        var all = await sut.GetAllAsync(CancellationToken.None);
        Assert.Equal(3, all.Count);

        var notRevoked = await sut.GetAllIsNotRevokedAsync(CancellationToken.None);
        Assert.Equal(new[] { 1, 3 }, notRevoked.Select(x => x.Id).OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task Getters_By_Id_Name_ClientId_And_System()
    {
        var (sut, ctx) = CreateSutWithContext();
        var list = new List<ClientApplication>
        {
            new() { Id = 1, Name = "sys", ClientId = "sys", IsSystem = true, IsRevoked = false },
            new() { Id = 2, Name = "user", ClientId = "user", IsSystem = false, IsRevoked = false },
            new() { Id = 3, Name = "revokedSys", ClientId = "sys2", IsSystem = true, IsRevoked = true }
        };
        await ctx.ClientApplications.AddRangeAsync(list);
        await ctx.SaveChangesAsync();

        Assert.Equal(2, (await sut.GetByIdAsync(2, CancellationToken.None))?.Id);
        Assert.Equal(1, (await sut.GetByNameAsync("sys", CancellationToken.None))?.Id);
        Assert.Equal(2, (await sut.GetByClientIdAsync("user", CancellationToken.None))?.Id);

        // only system and not revoked
        var system = await sut.GetBySystemByClientIdAsync("sys", CancellationToken.None);
        Assert.NotNull(system);
        Assert.Equal(1, system!.Id);

        // should be null because revoked
        var revokedSystem = await sut.GetBySystemByClientIdAsync("sys2", CancellationToken.None);
        Assert.Null(revokedSystem);

        // any system configured
        var isSystemConfigured = await sut.IsSystemConfiguredAsync(CancellationToken.None);
        Assert.NotNull(isSystemConfigured);
        Assert.True(isSystemConfigured!.IsSystem);
    }

    [Fact]
    public async Task GetPage_Works()
    {
        var (sut, ctx) = CreateSutWithContext();
        var items = Enumerable.Range(1, 25)
            .Select(i => new ClientApplication { Id = i, Name = $"n{i}", ClientId = $"c{i}" });
        await ctx.ClientApplications.AddRangeAsync(items);
        await ctx.SaveChangesAsync();

        var page = await sut.GetPageAsync(2, 10, CancellationToken.None);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(11, page.Items.First().Id);
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();
    }
}
