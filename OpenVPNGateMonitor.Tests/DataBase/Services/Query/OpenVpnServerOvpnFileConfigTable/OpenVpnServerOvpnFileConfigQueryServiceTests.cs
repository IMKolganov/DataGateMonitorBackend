using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;

public class OpenVpnServerOvpnFileConfigQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<OpenVpnServerOvpnFileConfig> OpenVpnServerOvpnFileConfigs => Set<OpenVpnServerOvpnFileConfig>();
    }

    private static List<OpenVpnServerOvpnFileConfig> CreateSample() => new()
    {
        new OpenVpnServerOvpnFileConfig { Id = 1, VpnServerId = 11, VpnServerIp = "10.0.0.1", VpnServerPort = 1194 },
        new OpenVpnServerOvpnFileConfig { Id = 2, VpnServerId = 22, VpnServerIp = "10.0.0.2", VpnServerPort = 1194 },
        new OpenVpnServerOvpnFileConfig { Id = 3, VpnServerId = 33, VpnServerIp = "10.0.0.3", VpnServerPort = 1194 },
    };

    private static (Mock<IQueryService<OpenVpnServerOvpnFileConfig, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<OpenVpnServerOvpnFileConfig> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.OpenVpnServerOvpnFileConfigs.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<OpenVpnServerOvpnFileConfig, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<OpenVpnServerOvpnFileConfig, object>>[]>()))
            .Returns(ctx.OpenVpnServerOvpnFileConfigs);
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

        var sut = new OpenVpnServerOvpnFileConfigQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var target = new OpenVpnServerOvpnFileConfig { Id = 42, VpnServerId = 777, VpnServerIp = "10.8.0.1" };
        var (q, ctx) = CreateEfBackedQuery(new[] { target });
        q.Setup(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerOvpnFileConfig, object>>[]>()))
         .ReturnsAsync(target)
         .Verifiable();

        var sut = new OpenVpnServerOvpnFileConfigQueryService(q.Object);
        var result = await sut.GetById(42, CancellationToken.None);

        Assert.Same(target, result);
        q.Verify(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerOvpnFileConfig, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByVpnServerIdIdAsync_Uses_Query_To_Return_Matching_Record()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new OpenVpnServerOvpnFileConfigQueryService(q.Object);

        var result = await sut.GetByVpnServerIdId(22, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(22, result!.VpnServerId);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task AnyByVpnServerId_Delegates_To_AnyAsync()
    {
        var (q, ctx) = CreateEfBackedQuery(Array.Empty<OpenVpnServerOvpnFileConfig>());
        q.Setup(x => x.Any(It.IsAny<Expression<Func<OpenVpnServerOvpnFileConfig, bool>>>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(true)
         .Verifiable();

        var sut = new OpenVpnServerOvpnFileConfigQueryService(q.Object);
        var found = await sut.AnyByVpnServerId(999, CancellationToken.None);

        Assert.True(found);
        q.Verify(x => x.Any(It.IsAny<Expression<Func<OpenVpnServerOvpnFileConfig, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        var paged = new TestPagedResult<OpenVpnServerOvpnFileConfig>
        {
            Page = 1,
            PageSize = 2,
            TotalCount = data.Count,
            Items = data.Take(2).ToList()
        };

        q.Setup(x => x.Page(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerOvpnFileConfig, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<OpenVpnServerOvpnFileConfig>)
         .Verifiable();

        var sut = new OpenVpnServerOvpnFileConfigQueryService(q.Object);
        var result = await sut.GetPage(1, 2, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.Page(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OpenVpnServerOvpnFileConfig, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
