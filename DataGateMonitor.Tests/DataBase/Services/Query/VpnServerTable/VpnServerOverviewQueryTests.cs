using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Repositories.Queries.Interfaces;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerTable;

public class VpnServerOverviewQueryTests
{
    // ---- helpers ----

    private static DbContextOptions<TestDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static (VpnServerOverviewQuery sut, TestDbContext ctx) CreateSutWithContext()
    {
        var options = CreateOptions();
        var ctx = new TestDbContext(options);

        var uowMock = new Mock<IUnitOfWork>();

        uowMock
            .Setup(u => u.GetQuery<VpnServer>())
            .Returns(new TestQuery<VpnServer>(ctx.VpnServers));

        uowMock
            .Setup(u => u.GetQuery<VpnServerClient>())
            .Returns(new TestQuery<VpnServerClient>(ctx.VpnServerClients));

        uowMock
            .Setup(u => u.GetQuery<VpnServerStatusLog>())
            .Returns(new TestQuery<VpnServerStatusLog>(ctx.VpnServerStatusLogs));

        uowMock
            .Setup(u => u.GetQuery<QuotaPlanAllowedServer>())
            .Returns(new TestQuery<QuotaPlanAllowedServer>(ctx.QuotaPlanAllowedServers));

        var sut = new VpnServerOverviewQuery(uowMock.Object);
        return (sut, ctx);
    }

    // ---- tests ----

    [Fact]
    public async Task GetAllVpnServersWithStatusAsync_Returns_AggregatedData_PerServer()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();

        var now = DateTimeOffset.UtcNow;

        var s1 = new VpnServer
        {
            Id = 1,
            ServerName = "srv1",
            IsOnline = true,
            IsDefault = true,
            ApiUrl = "https://srv1",
            CreateDate = now.AddHours(-2),
            LastUpdate = now,
            IsDeleted = false
        };

        var s2 = new VpnServer
        {
            Id = 2,
            ServerName = "srv2",
            IsOnline = false,
            IsDefault = false,
            ApiUrl = "https://srv2",
            CreateDate = now.AddHours(-5),
            LastUpdate = now.AddMinutes(-10),
            IsDeleted = false
        };

        await ctx.VpnServers.AddRangeAsync(s1, s2);

        var clients = new List<VpnServerClient>
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

        await ctx.VpnServerClients.AddRangeAsync(clients);

        var logs = new List<VpnServerStatusLog>
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

        await ctx.VpnServerStatusLogs.AddRangeAsync(logs);
        await ctx.SaveChangesAsync();

        // Act
        var result = await sut.GetAllVpnServersWithStatusAsync(ct: CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);

        var s1Dto = result.Single(x => x.VpnServerResponses.VpnServer.Id == 1);
        var s2Dto = result.Single(x => x.VpnServerResponses.VpnServer.Id == 2);

        // Server 1
        Assert.Equal("srv1", s1Dto.VpnServerResponses.VpnServer.ServerName);
        Assert.Equal(1, s1Dto.CountConnectedClients); // 1 connected
        Assert.Equal(2, s1Dto.CountSessions);          // 2 total
        Assert.Equal(100 + 300, s1Dto.TotalBytesIn);
        Assert.Equal(200 + 400, s1Dto.TotalBytesOut);
        Assert.NotNull(s1Dto.VpnServerStatusLogResponse);
        Assert.Equal(1, s1Dto.VpnServerStatusLogResponse!.VpnServerId);
        Assert.Equal("v1", s1Dto.VpnServerStatusLogResponse.Version);

        // Server 2
        Assert.Equal("srv2", s2Dto.VpnServerResponses.VpnServer.ServerName);
        Assert.Equal(1, s2Dto.CountConnectedClients); // 1 connected
        Assert.Equal(1, s2Dto.CountSessions);          // 1 total
        Assert.Equal(50, s2Dto.TotalBytesIn);
        Assert.Equal(70, s2Dto.TotalBytesOut);
        Assert.NotNull(s2Dto.VpnServerStatusLogResponse);
        Assert.Equal(2, s2Dto.VpnServerStatusLogResponse!.VpnServerId);
        Assert.Equal("v2", s2Dto.VpnServerStatusLogResponse.Version);
    }

    [Fact]
    public async Task GetVpnServerWithStatusAsync_Returns_SingleServerData()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();

        var now = DateTimeOffset.UtcNow;

        var s1 = new VpnServer
        {
            Id = 10,
            ServerName = "single",
            IsOnline = true,
            IsDefault = false,
            ApiUrl = "https://single",
            CreateDate = now.AddDays(-1),
            LastUpdate = now
        };

        var sOther = new VpnServer
        {
            Id = 99,
            ServerName = "other"
        };

        await ctx.VpnServers.AddRangeAsync(s1, sOther);

        await ctx.VpnServerClients.AddRangeAsync(new[]
        {
            new VpnServerClient { Id = 1, VpnServerId = 10, IsConnected = true },
            new VpnServerClient { Id = 2, VpnServerId = 10, IsConnected = true },
            new VpnServerClient { Id = 3, VpnServerId = 10, IsConnected = false },
            new VpnServerClient { Id = 4, VpnServerId = 99, IsConnected = true }
        });

        await ctx.VpnServerStatusLogs.AddRangeAsync(new[]
        {
            new VpnServerStatusLog
            {
                Id = 1, VpnServerId = 10, BytesIn = 10, BytesOut = 20, Version = "v1"
            },
            new VpnServerStatusLog
            {
                Id = 2, VpnServerId = 10, BytesIn = 30, BytesOut = 40, Version = "v2"
            }, // should be picked as last (Id desc)
            new VpnServerStatusLog
            {
                Id = 3, VpnServerId = 99, BytesIn = 999, BytesOut = 999, Version = "other"
            }
        });

        await ctx.SaveChangesAsync();

        // Act
        var dto = await sut.GetVpnServerWithStatusAsync(10, CancellationToken.None);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(10, dto.VpnServerResponses.VpnServer.Id);
        Assert.Equal("single", dto.VpnServerResponses.VpnServer.ServerName);

        // 3 clients total, 2 connected
        Assert.Equal(2, dto.CountConnectedClients);
        Assert.Equal(3, dto.CountSessions);

        // Only logs for server 10
        Assert.Equal(10 + 30, dto.TotalBytesIn);
        Assert.Equal(20 + 40, dto.TotalBytesOut);

        Assert.NotNull(dto.VpnServerStatusLogResponse);
        Assert.Equal(10, dto.VpnServerStatusLogResponse!.VpnServerId);
        Assert.Equal("v2", dto.VpnServerStatusLogResponse.Version); // last by Id desc
    }

    [Fact]
    public async Task GetVpnServerWithStatusAsync_Throws_WhenServerNotFound()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();

        // No servers saved
        await ctx.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => sut.GetVpnServerWithStatusAsync(777, CancellationToken.None));
    }

    [Fact]
    public async Task GetClientCountersAsync_Returns_CorrectCounts()
    {
        // Arrange
        var (sut, ctx) = CreateSutWithContext();

        await ctx.VpnServerClients.AddRangeAsync(new[]
        {
            new VpnServerClient { Id = 1, VpnServerId = 5, IsConnected = true },
            new VpnServerClient { Id = 2, VpnServerId = 5, IsConnected = false },
            new VpnServerClient { Id = 3, VpnServerId = 5, IsConnected = true },
            new VpnServerClient { Id = 4, VpnServerId = 99, IsConnected = true }
        });

        await ctx.SaveChangesAsync();

        // Act
        var (connected, sessions) = await sut.GetClientCountersAsync(5, CancellationToken.None);

        // Assert
        Assert.Equal(2, connected); // two connected for server 5
        Assert.Equal(3, sessions);  // three total for server 5
    }

    [Fact]
    public async Task GetAllVpnServersWithStatusAsync_WhenIncludeDeletedFalse_ExcludesDeletedServers()
    {
        var (sut, ctx) = CreateSutWithContext();
        var now = DateTimeOffset.UtcNow;

        var sActive = new VpnServer
        {
            Id = 100,
            ServerName = "active",
            IsOnline = true,
            IsDefault = false,
            ApiUrl = "https://active",
            CreateDate = now,
            LastUpdate = now,
            IsDeleted = false
        };
        var sDeleted = new VpnServer
        {
            Id = 101,
            ServerName = "deleted",
            IsOnline = false,
            IsDefault = false,
            ApiUrl = "https://deleted",
            CreateDate = now,
            LastUpdate = now,
            IsDeleted = true
        };
        await ctx.VpnServers.AddRangeAsync(sActive, sDeleted);
        await ctx.SaveChangesAsync();

        var result = await sut.GetAllVpnServersWithStatusAsync(includeDeleted: false, ct: CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(100, result[0].VpnServerResponses.VpnServer.Id);
        Assert.Equal("active", result[0].VpnServerResponses.VpnServer.ServerName);
    }

    [Fact]
    public async Task GetAllVpnServersWithStatusAsync_WhenIncludeDeletedTrue_IncludesDeletedServers()
    {
        var (sut, ctx) = CreateSutWithContext();
        var now = DateTimeOffset.UtcNow;

        var sActive = new VpnServer
        {
            Id = 200,
            ServerName = "active",
            IsDeleted = false,
            ApiUrl = "https://a",
            CreateDate = now,
            LastUpdate = now
        };
        var sDeleted = new VpnServer
        {
            Id = 201,
            ServerName = "deleted",
            IsDeleted = true,
            ApiUrl = "https://d",
            CreateDate = now,
            LastUpdate = now
        };
        await ctx.VpnServers.AddRangeAsync(sActive, sDeleted);
        await ctx.SaveChangesAsync();

        var result = await sut.GetAllVpnServersWithStatusAsync(includeDeleted: true, ct: CancellationToken.None);

        Assert.Equal(2, result.Count);
        var ids = result.Select(x => x.VpnServerResponses.VpnServer.Id).OrderBy(x => x).ToList();
        Assert.Equal([200, 201], ids);
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

    [Fact]
    public async Task GetAllVpnServersWithStatusAsync_WithRestrictToQuotaPlanId_ReturnsOnlyServersLinkedToPlan()
    {
        var (sut, ctx) = CreateSutWithContext();
        var now = DateTimeOffset.UtcNow;

        var sAllowed = new VpnServer
        {
            Id = 301,
            ServerName = "in-plan",
            IsDeleted = false,
            ApiUrl = "https://a",
            CreateDate = now,
            LastUpdate = now
        };
        var sOther = new VpnServer
        {
            Id = 302,
            ServerName = "not-in-plan",
            IsDeleted = false,
            ApiUrl = "https://b",
            CreateDate = now,
            LastUpdate = now
        };
        await ctx.VpnServers.AddRangeAsync(sAllowed, sOther);
        await ctx.QuotaPlanAllowedServers.AddAsync(new QuotaPlanAllowedServer
        {
            Id = 1,
            QuotaPlanId = 77,
            VpnServerId = 301,
            CreateDate = now,
            LastUpdate = now
        });
        await ctx.SaveChangesAsync();

        var result = await sut.GetAllVpnServersWithStatusAsync(
            includeDeleted: false,
            requireQuotaPlanAssignment: false,
            restrictToQuotaPlanId: 77,
            ct: CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(301, result[0].VpnServerResponses.VpnServer.Id);
    }

    [Fact]
    public async Task GetAllVpnServersWithStatusAsync_WithRequireQuotaPlanAssignment_ReturnsOnlyServersWithAnyQuotaLink()
    {
        var (sut, ctx) = CreateSutWithContext();
        var now = DateTimeOffset.UtcNow;

        var sLinked = new VpnServer
        {
            Id = 401,
            ServerName = "linked",
            IsDeleted = false,
            ApiUrl = "https://l",
            CreateDate = now,
            LastUpdate = now
        };
        var sOrphan = new VpnServer
        {
            Id = 402,
            ServerName = "orphan",
            IsDeleted = false,
            ApiUrl = "https://o",
            CreateDate = now,
            LastUpdate = now
        };
        await ctx.VpnServers.AddRangeAsync(sLinked, sOrphan);
        await ctx.QuotaPlanAllowedServers.AddAsync(new QuotaPlanAllowedServer
        {
            Id = 2,
            QuotaPlanId = 1,
            VpnServerId = 401,
            CreateDate = now,
            LastUpdate = now
        });
        await ctx.SaveChangesAsync();

        var result = await sut.GetAllVpnServersWithStatusAsync(
            includeDeleted: false,
            requireQuotaPlanAssignment: true,
            restrictToQuotaPlanId: null,
            ct: CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(401, result[0].VpnServerResponses.VpnServer.Id);
    }

    // ---- Test DbContext ----

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<VpnServer> VpnServers => Set<VpnServer>();
        public DbSet<VpnServerClient> VpnServerClients => Set<VpnServerClient>();
        public DbSet<VpnServerStatusLog> VpnServerStatusLogs => Set<VpnServerStatusLog>();
        public DbSet<QuotaPlanAllowedServer> QuotaPlanAllowedServers => Set<QuotaPlanAllowedServer>();
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
