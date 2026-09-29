using DataGateMonitor.Services.AvailabilityCheck;
using DataGateMonitor.Services.Others;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.AvailabilityCheck;

public class AvailabilityCheckEnvironmentTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("false", true)]
    [InlineData("0", true)]
    [InlineData("true", false)]
    [InlineData("TRUE", false)]
    [InlineData("1", false)]
    public void IsEnabled_RespectsDisabledVariable(string? raw, bool expected)
    {
        var prev = Environment.GetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable);
        try
        {
            Environment.SetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable, raw);
            AvailabilityCheckEnvironment.IsEnabled().Should().Be(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable, prev);
        }
    }
}

public class AvailabilityCheckSettingsKeysTests
{
    [Theory]
    [InlineData(0, 60)]
    [InlineData(30, 60)]
    [InlineData(60, 60)]
    [InlineData(300, 300)]
    [InlineData(86_400, 86_400)]
    [InlineData(100_000, 86_400)]
    public void ClampIntervalSeconds_Bounds(int input, int expected)
        => AvailabilityCheckSettingsKeys.ClampIntervalSeconds(input).Should().Be(expected);
}

public class AvailabilityCheckBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEnvDisabled_DoesNotCreateScope()
    {
        var prev = Environment.GetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable);
        var prevStartup = AvailabilityCheckBackgroundService.StartupDelay;
        try
        {
            Environment.SetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable, "true");
            AvailabilityCheckBackgroundService.StartupDelay = TimeSpan.Zero;

            var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
            var sut = new AvailabilityCheckBackgroundService(
                NullLogger<AvailabilityCheckBackgroundService>.Instance,
                scopeFactory.Object);

            await sut.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            await sut.StopAsync(CancellationToken.None);

            scopeFactory.Verify(f => f.CreateScope(), Times.Never);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable, prev);
            AvailabilityCheckBackgroundService.StartupDelay = prevStartup;
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_RunsCheckAtLeastOnce()
    {
        var prev = Environment.GetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable);
        var prevStartup = AvailabilityCheckBackgroundService.StartupDelay;
        try
        {
            Environment.SetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable, null);
            AvailabilityCheckBackgroundService.StartupDelay = TimeSpan.Zero;

            var runner = new Mock<IAvailabilityCheckRunner>();
            var ran = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            runner
                .Setup(r => r.RunAsync(It.IsAny<CancellationToken>(), false))
                .ReturnsAsync(new DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses.AvailabilityCheckStatusResponse())
                .Callback(() => ran.TrySetResult());

            var settings = new Mock<ISettingsService>();
            // Large interval so the loop doesn't spin after the first run.
            settings
                .Setup(s => s.GetValueAsync<int>(AvailabilityCheckSettingsKeys.IntervalSeconds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3600);

            var services = new ServiceCollection();
            services.AddSingleton(runner.Object);
            services.AddSingleton(settings.Object);
            await using var provider = services.BuildServiceProvider();

            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var sut = new AvailabilityCheckBackgroundService(
                NullLogger<AvailabilityCheckBackgroundService>.Instance,
                scopeFactory);

            await sut.StartAsync(CancellationToken.None);
            await ran.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await sut.StopAsync(CancellationToken.None);

            runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>(), false), Times.AtLeastOnce);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AvailabilityCheckEnvironment.DisabledVariable, prev);
            AvailabilityCheckBackgroundService.StartupDelay = prevStartup;
        }
    }
}
