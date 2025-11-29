using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query;

public class EfQueryServiceTests
{
    private static DbContextOptions<TestDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static (EfQueryService<ClientApplication, int> sut, TestDbContext ctx) CreateSutWithContext()
    {
        var ctx = new TestDbContext(CreateOptions());
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.GetQuery<ClientApplication>())
            .Returns(new TestQuery<ClientApplication>(ctx.ClientApplications));
        return (new EfQueryService<ClientApplication, int>(uow.Object), ctx);
    }

    [Fact]
    public async Task GetAll_Ordered_By_Id()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.ClientApplications.AddRangeAsync(
            new ClientApplication { Id = 2, Name = "b", ClientId = "b" },
            new ClientApplication { Id = 1, Name = "a", ClientId = "a" }
        );
        await ctx.SaveChangesAsync();

        var all = await sut.GetAllAsync(ct: CancellationToken.None);
        Assert.Equal(new[] { 1, 2 }, all.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task FindById_Works()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.ClientApplications.AddRangeAsync(
            new ClientApplication { Id = 1, Name = "a", ClientId = "a" }
        );
        await ctx.SaveChangesAsync();

        var e = await sut.FindByIdAsync(1, ct: CancellationToken.None);
        Assert.NotNull(e);
        Assert.Equal("a", e!.Name);
    }

    [Fact]
    public async Task Where_OrderBy_Works()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.ClientApplications.AddRangeAsync(
            new ClientApplication { Id = 1, Name = "b", ClientId = "x" },
            new ClientApplication { Id = 2, Name = "a", ClientId = "x" }
        );
        await ctx.SaveChangesAsync();

        var list = await sut.WhereAsync(x => x.ClientId == "x",
            orderBy: q => q.OrderBy(e => e.Name), ct: CancellationToken.None);
        Assert.Equal(new[] { 2, 1 }, list.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task PageAsync_Works()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.ClientApplications.AddRangeAsync(
            Enumerable.Range(1, 23).Select(i => new ClientApplication { Id = i, Name = $"n{i}", ClientId = $"c{i}" })
        );
        await ctx.SaveChangesAsync();

        var page = await sut.PageAsync(3, 10, ct: CancellationToken.None);
        Assert.Equal(3, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(23, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(new[] { 21, 22, 23 }, page.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Count_And_Any_Work()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.ClientApplications.AddRangeAsync(
            new ClientApplication { Id = 1, Name = "a", ClientId = "x" },
            new ClientApplication { Id = 2, Name = "b", ClientId = "y" }
        );
        await ctx.SaveChangesAsync();

        Assert.Equal(2, await sut.CountAsync(ct: CancellationToken.None));
        Assert.True(await sut.AnyAsync(x => x.ClientId == "x", ct: CancellationToken.None));
        Assert.False(await sut.AnyAsync(x => x.ClientId == "z", ct: CancellationToken.None));
    }

    [Fact]
    public async Task FirstOrDefault_With_Predicate_And_Order_Works()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.ClientApplications.AddRangeAsync(
            new ClientApplication { Id = 1, Name = "b", ClientId = "x" },
            new ClientApplication { Id = 2, Name = "a", ClientId = "x" }
        );
        await ctx.SaveChangesAsync();

        var first = await sut.FirstOrDefaultAsync(x => x.ClientId == "x",
            orderBy: q => q.OrderBy(e => e.Name), ct: CancellationToken.None);
        Assert.NotNull(first);
        Assert.Equal(2, first!.Id);
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();
    }
}
