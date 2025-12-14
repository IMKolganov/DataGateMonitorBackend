using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.ClientApplicationTable;

public class ClientApplicationQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // The production model has conflicting [Key] attributes (Id from BaseEntity and ClientId here).
            // Configure a composite key to satisfy EF Core in the test context.
            modelBuilder.Entity<ClientApplication>().HasKey(x => new { x.Id, x.ClientId });
        }
    }

    private static List<ClientApplication> CreateSample()
        => new()
        {
            new ClientApplication { Id = 1, Name = "AppA", ClientId = "cid-a", IsSystem = false, IsRevoked = false },
            new ClientApplication { Id = 2, Name = "AppB", ClientId = "cid-b", IsSystem = true,  IsRevoked = false },
            new ClientApplication { Id = 3, Name = "AppC", ClientId = "cid-c", IsSystem = true,  IsRevoked = true  },
            new ClientApplication { Id = 4, Name = "AppD", ClientId = "cid-d", IsSystem = false, IsRevoked = true  },
        };

    private static (Mock<IQueryService<ClientApplication, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<ClientApplication> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.ClientApplications.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<ClientApplication, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<ClientApplication, object>>[]>()))
            .Returns(ctx.ClientApplications);
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

        var sut = new ClientApplicationQueryService(q.Object);

        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllIsNotRevokedAsync_Filters_IsRevoked_False()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new ClientApplicationQueryService(q.Object);

        var result = await sut.GetAllIsNotRevoked(CancellationToken.None);

        Assert.All(result, x => Assert.False(x.IsRevoked));
        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id).OrderBy(i => i));
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var target = new ClientApplication { Id = 42, Name = "X" };
        var (q, ctx) = CreateEfBackedQuery(new[] { target });
        q.Setup(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<ClientApplication, object>>[]>()))
         .ReturnsAsync(target)
         .Verifiable();

        var sut = new ClientApplicationQueryService(q.Object);
        var result = await sut.GetById(42, CancellationToken.None);

        Assert.Same(target, result);
        q.Verify(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<ClientApplication, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByNameAsync_Returns_Entity_By_Name()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new ClientApplicationQueryService(q.Object);

        var result = await sut.GetByName("AppB", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByClientIdAsync_Returns_Entity_By_ClientId()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new ClientApplicationQueryService(q.Object);

        var result = await sut.GetByClientId("cid-c", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetBySystemByClientIdAsync_Filters_By_ClientId_IsSystem_And_NotRevoked()
    {
        var data = new List<ClientApplication>
        {
            new() { Id = 10, ClientId = "same", IsSystem = false, IsRevoked = false },
            new() { Id = 11, ClientId = "same", IsSystem = true,  IsRevoked = true  },
            new() { Id = 12, ClientId = "same", IsSystem = true,  IsRevoked = false }, // expected
        };

        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new ClientApplicationQueryService(q.Object);

        var result = await sut.GetBySystemByClientId("same", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(12, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task IsSystemConfiguredAsync_Returns_First_System_App()
    {
        var data = new List<ClientApplication>
        {
            new() { Id = 1, IsSystem = false },
            new() { Id = 2, IsSystem = true  }, // first system
            new() { Id = 3, IsSystem = true  },
        };

        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new ClientApplicationQueryService(q.Object);

        var result = await sut.IsSystemConfigured(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        var paged = new TestPagedResult<ClientApplication>
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 25,
            Items = data.Take(2).ToList()
        };

        q.Setup(x => x.Page(2, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<ClientApplication, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<ClientApplication>)
         .Verifiable();

        var sut = new ClientApplicationQueryService(q.Object);
        var result = await sut.GetPage(2, 10, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.Page(2, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<ClientApplication, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
