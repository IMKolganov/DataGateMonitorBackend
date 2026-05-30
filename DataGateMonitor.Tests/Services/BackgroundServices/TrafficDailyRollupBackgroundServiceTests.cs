using DataGateMonitor.Services.BackgroundServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.BackgroundServices;

public class TrafficDailyRollupBackgroundServiceTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("false", true)]
    [InlineData("FALSE", true)]
    [InlineData("true", false)]
    [InlineData("TRUE", false)]
    public void IsEnabled_RespectsDisableFlag(string? envValue, bool expected)
    {
        var key = "TRAFFIC_DAILY_ROLLUP_DISABLED";
        var previous = Environment.GetEnvironmentVariable(key);
        try
        {
            if (envValue is null)
                Environment.SetEnvironmentVariable(key, null);
            else
                Environment.SetEnvironmentVariable(key, envValue);

            Assert.Equal(expected, TrafficDailyRollupEnvironment.IsEnabled());
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void IsBackfillOnStartEnabled_RespectsFlag(string? envValue, bool expected)
    {
        var key = "TRAFFIC_DAILY_BACKFILL_ON_START";
        var previous = Environment.GetEnvironmentVariable(key);
        try
        {
            if (envValue is null)
                Environment.SetEnvironmentVariable(key, null);
            else
                Environment.SetEnvironmentVariable(key, envValue);

            Assert.Equal(expected, TrafficDailyRollupEnvironment.IsBackfillOnStartEnabled());
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotInvokeRunner()
    {
        var key = "TRAFFIC_DAILY_ROLLUP_DISABLED";
        var previous = Environment.GetEnvironmentVariable(key);
        var runner = new Mock<ITrafficDailyRollupRunner>();

        try
        {
            Environment.SetEnvironmentVariable(key, "true");
            var sut = new TrafficDailyRollupBackgroundService(runner.Object, NullLogger<TrafficDailyRollupBackgroundService>.Instance);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            await sut.StartAsync(cts.Token);
            await Task.Delay(80);
            await sut.StopAsync(CancellationToken.None);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }

        runner.Verify(r => r.RunCatchUpThroughYesterdayAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
