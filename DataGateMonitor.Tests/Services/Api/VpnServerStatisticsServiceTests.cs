using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Tests.Helpers;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api;

public class VpnServerStatisticsServiceTests
{
    [Fact]
    public async Task GetTrafficGroupedByClientAsync_Returns_ClientTraffics_WithTelegramInfo()
    {
        var options = new DbContextOptionsBuilder<VpnStatsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new VpnStatsTestDbContext(options);
        var clients = new List<VpnServerClient>
        {
            new() { Id = 1, VpnServerId = 10, ExternalId = "123", BytesReceived = 1000, BytesSent = 2000 },
            new() { Id = 2, VpnServerId = 10, ExternalId = "123", BytesReceived = 500, BytesSent = 500 },
            new() { Id = 3, VpnServerId = 10, ExternalId = "456", BytesReceived = 1048576, BytesSent = 0 }
        };
        var telegramUsers = new List<TelegramBotUser>
        {
            new() { Id = 1, TelegramId = 123, Username = "u1", FirstName = "F1", LastName = "L1" }
        };
        ctx.VpnServerClients.AddRange(clients);
        ctx.TelegramBotUsers.AddRange(telegramUsers);
        await ctx.SaveChangesAsync();

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.GetQuery<VpnServerClient>()).Returns(new TestQuery<VpnServerClient>(ctx.VpnServerClients));
        uow.Setup(u => u.GetQuery<TelegramBotUser>()).Returns(new TestQuery<TelegramBotUser>(ctx.TelegramBotUsers));

        var sut = new VpnServerStatisticsService(uow.Object);

        var result = await sut.GetTrafficGroupedByClientAsync(10, CancellationToken.None);

        Assert.NotNull(result.ClientTraffics);
        Assert.Equal(2, result.ClientTraffics.Count); // two distinct ExternalIds: 123, 456
        var by123 = result.ClientTraffics.FirstOrDefault(c => c.ExternalId == "123");
        var by456 = result.ClientTraffics.FirstOrDefault(c => c.ExternalId == "456");
        Assert.NotNull(by123);
        Assert.NotNull(by456);
        Assert.Equal(Math.Round(4000 / 1048576.0, 2), by123!.TotalMbTraffic);
        Assert.Equal("u1", by123.TgUsername);
        Assert.Equal("F1", by123.TgFirstName);
        Assert.Equal("L1", by123.TgLastName);
        Assert.Equal(Math.Round(1048576 / 1048576.0, 2), by456!.TotalMbTraffic);
        Assert.Null(by456.TgUsername); // no telegram user for 456
    }

    private sealed class VpnStatsTestDbContext : DbContext
    {
        public VpnStatsTestDbContext(DbContextOptions<VpnStatsTestDbContext> options) : base(options) { }
        public DbSet<VpnServerClient> VpnServerClients => Set<VpnServerClient>();
        public DbSet<TelegramBotUser> TelegramBotUsers => Set<TelegramBotUser>();
    }

    [Fact]
    public async Task GetGroupedConnectionsByLocationAsync_Throws_NotImplementedException()
    {
        var uow = new Mock<IUnitOfWork>();
        var sut = new VpnServerStatisticsService(uow.Object);

        await Assert.ThrowsAsync<NotImplementedException>(
            () => sut.GetGroupedConnectionsByLocationAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task GetAverageSessionDurationAsync_Throws_NotImplementedException()
    {
        var uow = new Mock<IUnitOfWork>();
        var sut = new VpnServerStatisticsService(uow.Object);

        await Assert.ThrowsAsync<NotImplementedException>(
            () => sut.GetAverageSessionDurationAsync(1, CancellationToken.None));
    }
}
