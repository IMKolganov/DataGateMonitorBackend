using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class VpnServerClientOverviewQueryTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<VpnServerClient> Clients => Set<VpnServerClient>();
    }

    private static (Mock<IUnitOfWork> uow, TestDbContext ctx) CreateUowWithData(IEnumerable<VpnServerClient> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.Clients.AddRange(data);
        ctx.SaveChanges();

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.GetQuery<VpnServerClient>())
           .Returns(new TestQuery<VpnServerClient>(ctx.Clients));
        return (uow, ctx);
    }

    [Fact]
    public async Task GetAllConnectedVpnServerClientsAsync_Paginates_And_Enriches_DisplayName()
    {
        var data = new List<VpnServerClient>
        {
            new() { Id = 1, VpnServerId = 1, IsConnected = true,  ExternalId = "ext-1", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-3) },
            new() { Id = 2, VpnServerId = 1, IsConnected = false, ExternalId = "ext-2", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-2) },
            new() { Id = 3, VpnServerId = 1, IsConnected = true,  ExternalId = "ext-3", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-1) },
            new() { Id = 4, VpnServerId = 2, IsConnected = true,  ExternalId = "ext-x", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-1) },
        };

        var (uow, ctx) = CreateUowWithData(data);

        var users = new Mock<IUserQueryService>();
        users.Setup(x => x.GetByExternalId("ext-1", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new User { Id = 10, DisplayName = "DN-ext-1" });
        users.Setup(x => x.GetByExternalId("ext-3", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new User { Id = 11, DisplayName = "DN-ext-3" });

        var sut = new VpnServerClientOverviewQuery(uow.Object, users.Object);

        // Only serverId=1 & connected => Ids {3,1}, order desc, take pageSize=1 => item Id=3
        var resp = await sut.GetAllConnectedVpnServerClientsAsync(1, page: 1, pageSize: 1, CancellationToken.None);

        Assert.Equal(2, resp.TotalCount);
        Assert.Single(resp.VpnClientInfoResponse);
        var item = resp.VpnClientInfoResponse[0];
        Assert.Equal(3, item.Id);
        Assert.Equal("DN-ext-3", item.DisplayName);

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllHistoryVpnServerClientsAsync_Paginates_All_And_Enriches()
    {
        var data = new List<VpnServerClient>
        {
            new() { Id = 10, VpnServerId = 5, IsConnected = false, ExternalId = "uA", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-3) },
            new() { Id = 11, VpnServerId = 5, IsConnected = true,  ExternalId = "uB", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-2) },
            new() { Id = 12, VpnServerId = 5, IsConnected = true,  ExternalId = "uC", ConnectedSince = DateTimeOffset.UtcNow.AddHours(-1) },
        };

        var (uow, ctx) = CreateUowWithData(data);
        var users = new Mock<IUserQueryService>();
        users.Setup(x => x.GetByExternalId(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string extId, CancellationToken _) => new User { Id = 100, DisplayName = $"DN-{extId}" });

        var sut = new VpnServerClientOverviewQuery(uow.Object, users.Object);

        // History for serverId 5: total 3; page 2 size 2 => only one item (Id=10 order desc -> [12,11] then [10])
        var resp = await sut.GetAllHistoryVpnServerClientsAsync(5, page: 2, pageSize: 2, CancellationToken.None);

        Assert.Equal(3, resp.TotalCount);
        Assert.Single(resp.VpnClientInfoResponse);
        var item = resp.VpnClientInfoResponse[0];
        Assert.Equal(10, item.Id);
        Assert.Equal("DN-uA", item.DisplayName);

        await ctx.DisposeAsync();
    }
}
