using DataGateMonitor.Configurations;
using DataGateMonitor.Services.Cache;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using StackExchange.Redis;

namespace DataGateMonitor.Tests.Configurations;

public class ApplicationRedisStatusTests
{
    [Fact]
    public async Task ResolveAsync_WhenNotConfigured_HidesRedis()
    {
        var config = new ConfigurationBuilder().Build();
        var provider = new Mock<IRedisDatabaseProvider>(MockBehavior.Strict);

        var result = await ApplicationRedisStatus.ResolveAsync(config, provider.Object);

        result.IsConfigured.Should().BeFalse();
        provider.Verify(
            p => p.GetDatabaseAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_WhenConnected_ReturnsOkWithPing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379",
            })
            .Build();

        var database = new Mock<IDatabase>(MockBehavior.Strict);
        database.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(3));

        var provider = new Mock<IRedisDatabaseProvider>(MockBehavior.Strict);
        provider.Setup(p => p.GetDatabaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(database.Object);

        var result = await ApplicationRedisStatus.ResolveAsync(config, provider.Object);

        result.IsConfigured.Should().BeTrue();
        result.Tone.Should().Be("ok");
        result.Line.Should().Be("Connected (ping 3 ms).");
    }

    [Fact]
    public async Task ResolveAsync_WhenUnavailable_ReturnsError()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = "localhost:6379",
            })
            .Build();

        var provider = new Mock<IRedisDatabaseProvider>(MockBehavior.Strict);
        provider.Setup(p => p.GetDatabaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDatabase?)null);

        var result = await ApplicationRedisStatus.ResolveAsync(config, provider.Object);

        result.IsConfigured.Should().BeTrue();
        result.Tone.Should().Be("error");
        result.Line.Should().Contain("unavailable");
    }
}
