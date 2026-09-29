using DataGateMonitor.Services.RfAvailability;
using DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Dto;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.RfAvailability;

public class RfAvailabilityProbeClientTests
{
    [Fact]
    public void BuildProbeUrl_encodes_target()
    {
        var url = RfAvailabilityProbeClient.BuildProbeUrl("https://xs1-hel.datagateapp.com:9443/");
        url.Should().Be(
            "https://status.rackot.ru/check.cgi?target=https%3A%2F%2Fxs1-hel.datagateapp.com%3A9443%2F");
    }
}

public class RfAvailabilityNotificationTrackerTests
{
    [Fact]
    public void Unreachable_dedupes_same_summary_and_clears_on_recovery()
    {
        var tracker = new RfAvailabilityNotificationTracker();
        const string target = "https://example.com/";

        tracker.TryMarkUnreachableNotified(target, "unreachable").Should().BeTrue();
        tracker.TryMarkUnreachableNotified(target, "unreachable").Should().BeFalse();
        tracker.HasUnreachableNotification(target).Should().BeTrue();

        tracker.ClearUnreachable(target);
        tracker.HasUnreachableNotification(target).Should().BeFalse();
        tracker.TryMarkUnreachableNotified(target, "unreachable").Should().BeTrue();
    }
}

public class RfAvailabilityCheckRunnerTests
{
    [Fact]
    public async Task RunAsync_skips_probe_when_disabled_unless_forced()
    {
        var settings = new Mock<DataGateMonitor.Services.Others.ISettingsService>();
        settings.Setup(s => s.GetValueAsync<bool>(RfAvailabilitySettingsKeys.Enabled, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        settings.Setup(s => s.GetValueAsync<string>(RfAvailabilitySettingsKeys.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/");

        var probe = new Mock<IRfAvailabilityProbeClient>();
        var store = new RfAvailabilityStatusStore();
        var notifications = new Mock<IRfAvailabilityNotificationService>();
        var tracker = new RfAvailabilityNotificationTracker();

        var runner = new RfAvailabilityCheckRunner(
            NullLogger<RfAvailabilityCheckRunner>.Instance,
            settings.Object,
            probe.Object,
            store,
            notifications.Object,
            tracker);

        var disabled = await runner.RunAsync(CancellationToken.None);
        disabled.Enabled.Should().BeFalse();
        disabled.TargetUrl.Should().Be("https://example.com/");
        probe.Verify(p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        probe.Setup(p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new RfAvailabilityProbeResultDto
            {
                Reachable = true,
                Summary = "reachable",
                Input = "https://example.com/",
                Host = "example.com",
            }, (string?)null, 42L));

        var forced = await runner.RunAsync(CancellationToken.None, force: true);
        forced.LastResult!.Reachable.Should().BeTrue();
        forced.LastDurationMs.Should().Be(42);
        probe.Verify(p => p.ProbeAsync("https://example.com/", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_notifies_once_when_unreachable_then_on_recovery()
    {
        var settings = new Mock<DataGateMonitor.Services.Others.ISettingsService>();
        settings.Setup(s => s.GetValueAsync<bool>(RfAvailabilitySettingsKeys.Enabled, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        settings.Setup(s => s.GetValueAsync<string>(RfAvailabilitySettingsKeys.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/");

        var probe = new Mock<IRfAvailabilityProbeClient>();
        probe.SetupSequence(p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new RfAvailabilityProbeResultDto { Reachable = false, Summary = "unreachable" }, null, 10L))
            .ReturnsAsync((new RfAvailabilityProbeResultDto { Reachable = false, Summary = "unreachable" }, null, 11L))
            .ReturnsAsync((new RfAvailabilityProbeResultDto { Reachable = true, Summary = "reachable" }, null, 12L));

        var notifications = new Mock<IRfAvailabilityNotificationService>();
        var runner = new RfAvailabilityCheckRunner(
            NullLogger<RfAvailabilityCheckRunner>.Instance,
            settings.Object,
            probe.Object,
            new RfAvailabilityStatusStore(),
            notifications.Object,
            new RfAvailabilityNotificationTracker());

        await runner.RunAsync(CancellationToken.None);
        await runner.RunAsync(CancellationToken.None);
        await runner.RunAsync(CancellationToken.None);

        notifications.Verify(
            n => n.NotifyUnreachableAsync("https://example.com/", "unreachable", It.IsAny<CancellationToken>()),
            Times.Once);
        notifications.Verify(
            n => n.NotifyRecoveredAsync("https://example.com/", "reachable", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
