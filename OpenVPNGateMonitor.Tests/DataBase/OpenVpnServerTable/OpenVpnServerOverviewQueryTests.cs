using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Tests.DataBase.OpenVpnServerTable;

public class OpenVpnServerOverviewQueryTests
{
    // ---- helpers ----

    private static DbContextOptions<TestDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static (OpenVpnServerOverviewQuery sut, TestDbContext ctx) CreateSutWithContext()
    {
        var options = CreateOptions();
        var ctx = new TestDbContext(options);

        var uowMock = new Mock<IUnitOfWork>();

        uowMock
            .Setup(u => u.GetQuery<OpenVpnServer>())
            .Returns(new TestQuery<OpenVpnServer>(ctx.OpenVpnServers));

        uowMock
            .Setup(u => u.GetQuery<OpenVpnServerClient>())
            .Returns(new TestQuery<OpenVpnServerClient>(ctx.OpenVpnServerClients));

        uowMock
            .Setup(u => u.GetQuery<OpenVpnServerStatusLog>())
            .Returns(new TestQuery<OpenVpnServerStatusLog>(ctx.OpenVpnServerStatusLogs));

        var sut = new OpenVpnServerOverviewQuery(uowMock.Object);
        return (sut, ctx);
    }

    // ---- tests ----

    [Fact]
    public async Task GetAllOpenVpnServersWithStatusAsync_Returns_AggregatedData_PerServer()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();

        var now = DateTimeOffset.UtcNow;

        var s1 = new OpenVpnServer
        {
            Id = 1,
            ServerName = "srv1",
            IsOnline = true,
            IsDefault = true,
            ApiUrl = "https://srv1",
            CreateDate = now.AddHours(-2),
            LastUpdate = now
        };

        var s2 = new OpenVpnServer
        {
            Id = 2,
            ServerName = "srv2",
            IsOnline = false,
            IsDefault = false,
            ApiUrl = "https://srv2",
            CreateDate = now.AddHours(-5),
            LastUpdate = now.AddMinutes(-10)
        };

        await ctx.OpenVpnServers.AddRangeAsync(s1, s2);

        var clients = new List<OpenVpnServerClient>
        {
            new()
            {
                Id = 1, VpnServerId = 1, IsConnected = true
            },
            new()
            {
                Id = 2, VpnServerId = 1, IsConnected = false
            },
            new()
            {
                Id = 3, VpnServerId = 2, IsConnected = true
            }
        };

        await ctx.OpenVpnServerClients.AddRangeAsync(clients);

        var logs = new List<OpenVpnServerStatusLog>
        {
            new()
            {
                Id = 1,
                VpnServerId = 1,
                SessionId = Guid.NewGuid(),
                BytesIn = 100,
                BytesOut = 200,
                Version = "v1"
            },
            new()
            {
                Id = 2,
                VpnServerId = 1,
                SessionId = Guid.NewGuid(),
                BytesIn = 300,
                BytesOut = 400,
                Version = "v1"
            },
            new()
            {
                Id = 3,
                VpnServerId = 2,
                SessionId = Guid.NewGuid(),
                BytesIn = 50,
                BytesOut = 70,
                Version = "v2"
            }
        };

        await ctx.OpenVpnServerStatusLogs.AddRangeAsync(logs);
        await ctx.SaveChangesAsync();

        // Act
        var result = await sut.GetAllOpenVpnServersWithStatusAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);

        var s1Dto = result.Single(x => x.OpenVpnServerResponses.OpenVpnServer.Id == 1);
        var s2Dto = result.Single(x => x.OpenVpnServerResponses.OpenVpnServer.Id == 2);

        // Server 1
        Assert.Equal("srv1", s1Dto.OpenVpnServerResponses.OpenVpnServer.ServerName);
        Assert.Equal(1, s1Dto.CountConnectedClients); // 1 connected
        Assert.Equal(2, s1Dto.CountSessions);          // 2 total
        Assert.Equal(100 + 300, s1Dto.TotalBytesIn);
        Assert.Equal(200 + 400, s1Dto.TotalBytesOut);
        Assert.NotNull(s1Dto.OpenVpnServerStatusLogResponse);
        Assert.Equal(1, s1Dto.OpenVpnServerStatusLogResponse!.VpnServerId);
        Assert.Equal("v1", s1Dto.OpenVpnServerStatusLogResponse.Version);

        // Server 2
        Assert.Equal("srv2", s2Dto.OpenVpnServerResponses.OpenVpnServer.ServerName);
        Assert.Equal(1, s2Dto.CountConnectedClients); // 1 connected
        Assert.Equal(1, s2Dto.CountSessions);          // 1 total
        Assert.Equal(50, s2Dto.TotalBytesIn);
        Assert.Equal(70, s2Dto.TotalBytesOut);
        Assert.NotNull(s2Dto.OpenVpnServerStatusLogResponse);
        Assert.Equal(2, s2Dto.OpenVpnServerStatusLogResponse!.VpnServerId);
        Assert.Equal("v2", s2Dto.OpenVpnServerStatusLogResponse.Version);
    }

    [Fact]
    public async Task GetOpenVpnServerWithStatusAsync_Returns_SingleServerData()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();

        var now = DateTimeOffset.UtcNow;

        var s1 = new OpenVpnServer
        {
            Id = 10,
            ServerName = "single",
            IsOnline = true,
            IsDefault = false,
            ApiUrl = "https://single",
            CreateDate = now.AddDays(-1),
            LastUpdate = now
        };

        var sOther = new OpenVpnServer
        {
            Id = 99,
            ServerName = "other"
        };

        await ctx.OpenVpnServers.AddRangeAsync(s1, sOther);

        await ctx.OpenVpnServerClients.AddRangeAsync(new[]
        {
            new OpenVpnServerClient { Id = 1, VpnServerId = 10, IsConnected = true },
            new OpenVpnServerClient { Id = 2, VpnServerId = 10, IsConnected = true },
            new OpenVpnServerClient { Id = 3, VpnServerId = 10, IsConnected = false },
            new OpenVpnServerClient { Id = 4, VpnServerId = 99, IsConnected = true }
        });

        await ctx.OpenVpnServerStatusLogs.AddRangeAsync(new[]
        {
            new OpenVpnServerStatusLog
            {
                Id = 1, VpnServerId = 10, BytesIn = 10, BytesOut = 20, Version = "v1"
            },
            new OpenVpnServerStatusLog
            {
                Id = 2, VpnServerId = 10, BytesIn = 30, BytesOut = 40, Version = "v2"
            }, // should be picked as last (Id desc)
            new OpenVpnServerStatusLog
            {
                Id = 3, VpnServerId = 99, BytesIn = 999, BytesOut = 999, Version = "other"
            }
        });

        await ctx.SaveChangesAsync();

        // Act
        var dto = await sut.GetOpenVpnServerWithStatusAsync(10, CancellationToken.None);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(10, dto.OpenVpnServerResponses.OpenVpnServer.Id);
        Assert.Equal("single", dto.OpenVpnServerResponses.OpenVpnServer.ServerName);

        // 3 clients total, 2 connected
        Assert.Equal(2, dto.CountConnectedClients);
        Assert.Equal(3, dto.CountSessions);

        // Only logs for server 10
        Assert.Equal(10 + 30, dto.TotalBytesIn);
        Assert.Equal(20 + 40, dto.TotalBytesOut);

        Assert.NotNull(dto.OpenVpnServerStatusLogResponse);
        Assert.Equal(10, dto.OpenVpnServerStatusLogResponse!.VpnServerId);
        Assert.Equal("v2", dto.OpenVpnServerStatusLogResponse.Version); // last by Id desc
    }

    [Fact]
    public async Task GetOpenVpnServerWithStatusAsync_Throws_WhenServerNotFound()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();

        // No servers saved
        await ctx.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => sut.GetOpenVpnServerWithStatusAsync(777, CancellationToken.None));
    }

    [Fact]
    public async Task GetClientCountersAsync_Returns_CorrectCounts()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();

        await ctx.OpenVpnServerClients.AddRangeAsync(new[]
        {
            new OpenVpnServerClient { Id = 1, VpnServerId = 5, IsConnected = true },
            new OpenVpnServerClient { Id = 2, VpnServerId = 5, IsConnected = false },
            new OpenVpnServerClient { Id = 3, VpnServerId = 5, IsConnected = true },
            new OpenVpnServerClient { Id = 4, VpnServerId = 99, IsConnected = true }
        });

        await ctx.SaveChangesAsync();

        // Act
        var (connected, sessions) = await sut.GetClientCountersAsync(5, CancellationToken.None);

        // Assert
        Assert.Equal(2, connected); // two connected for server 5
        Assert.Equal(3, sessions);  // three total for server 5
    }

    [Fact]
    public async Task GetClientCountersAsync_WhenNoClients_ReturnsZeroes()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();
        await ctx.SaveChangesAsync(); // ensure empty

        // Act
        var (connected, sessions) = await sut.GetClientCountersAsync(123, CancellationToken.None);

        // Assert
        Assert.Equal(0, connected);
        Assert.Equal(0, sessions);
    }

    // ---- Test DbContext ----

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<OpenVpnServer> OpenVpnServers => Set<OpenVpnServer>();
        public DbSet<OpenVpnServerClient> OpenVpnServerClients => Set<OpenVpnServerClient>();
        public DbSet<OpenVpnServerStatusLog> OpenVpnServerStatusLogs => Set<OpenVpnServerStatusLog>();
    }

    // ---- IQuery adapter for tests ----

    private sealed class TestQuery<TEntity> : IQuery<TEntity>
        where TEntity : class
    {
        private readonly IQueryable<TEntity> _query;

        public TestQuery(IQueryable<TEntity> query)
        {
            _query = query;
        }

        public IQueryable<TEntity> AsQueryable() => _query;
    }
}
