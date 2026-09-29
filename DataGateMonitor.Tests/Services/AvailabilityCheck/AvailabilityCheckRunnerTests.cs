using DataGateMonitor.Services.AvailabilityCheck;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.AvailabilityCheck;

public class AvailabilityCheckProbeClientTests
{
    [Fact]
    public void BuildProbeRequestUrl_encodes_target_and_strips_existing_query()
    {
        var url = AvailabilityCheckProbeClient.BuildProbeRequestUrl(
            "https://status.rackot.ru/check.cgi?foo=1",
            "https://xs1-hel.datagateapp.com:9443/");
        url.Should().Be(
            "https://status.rackot.ru/check.cgi?target=https%3A%2F%2Fxs1-hel.datagateapp.com%3A9443%2F");
    }
}

public class AvailabilityCheckNotificationTrackerTests
{
    [Fact]
    public void Unreachable_dedupes_same_summary_and_clears_on_recovery()
    {
        var tracker = new AvailabilityCheckNotificationTracker();
        const string target = "https://example.com/";

        tracker.TryMarkUnreachableNotified(target, "unreachable").Should().BeTrue();
        tracker.TryMarkUnreachableNotified(target, "unreachable").Should().BeFalse();
        tracker.HasUnreachableNotification(target).Should().BeTrue();

        tracker.ClearUnreachable(target);
        tracker.HasUnreachableNotification(target).Should().BeFalse();
        tracker.TryMarkUnreachableNotified(target, "unreachable").Should().BeTrue();
    }
}

public class AvailabilityCheckRunnerTests
{
    [Fact]
    public async Task RunAsync_skips_probe_when_disabled_unless_forced()
    {
        var settings = new Mock<DataGateMonitor.Services.Others.ISettingsService>();
        settings.Setup(s => s.GetValueAsync<bool>(AvailabilityCheckSettingsKeys.Enabled, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        settings.Setup(s => s.GetValueAsync<string>(AvailabilityCheckSettingsKeys.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/");
        settings.Setup(s => s.GetValueAsync<string>(AvailabilityCheckSettingsKeys.ProbeUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var probe = new Mock<IAvailabilityCheckProbeClient>();
        var runner = new AvailabilityCheckRunner(
            NullLogger<AvailabilityCheckRunner>.Instance,
            settings.Object,
            probe.Object,
            new AvailabilityCheckStatusStore(),
            new Mock<IAvailabilityCheckNotificationService>().Object,
            new AvailabilityCheckNotificationTracker());

        var disabled = await runner.RunAsync(CancellationToken.None);
        disabled.Enabled.Should().BeFalse();
        disabled.TargetUrl.Should().Be("https://example.com/");
        disabled.ProbeUrl.Should().Be(AvailabilityCheckSettingsKeys.DefaultProbeUrl);
        probe.Verify(
            p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        probe.Setup(p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new AvailabilityProbeResultDto
            {
                Reachable = true,
                Summary = "reachable",
                Input = "https://example.com/",
                Host = "example.com",
            }, (string?)null, 42L));

        var forced = await runner.RunAsync(CancellationToken.None, force: true);
        forced.LastResult!.Reachable.Should().BeTrue();
        forced.LastDurationMs.Should().Be(42);
        probe.Verify(
            p => p.ProbeAsync(
                AvailabilityCheckSettingsKeys.DefaultProbeUrl,
                "https://example.com/",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_notifies_once_when_unreachable_then_on_recovery()
    {
        var settings = new Mock<DataGateMonitor.Services.Others.ISettingsService>();
        settings.Setup(s => s.GetValueAsync<bool>(AvailabilityCheckSettingsKeys.Enabled, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        settings.Setup(s => s.GetValueAsync<string>(AvailabilityCheckSettingsKeys.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/");
        settings.Setup(s => s.GetValueAsync<string>(AvailabilityCheckSettingsKeys.ProbeUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://custom-probe.example/check.cgi");

        var probe = new Mock<IAvailabilityCheckProbeClient>();
        probe.SetupSequence(p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new AvailabilityProbeResultDto { Reachable = false, Summary = "unreachable" }, null, 10L))
            .ReturnsAsync((new AvailabilityProbeResultDto { Reachable = false, Summary = "unreachable" }, null, 11L))
            .ReturnsAsync((new AvailabilityProbeResultDto { Reachable = true, Summary = "reachable" }, null, 12L));

        var notifications = new Mock<IAvailabilityCheckNotificationService>();
        var runner = new AvailabilityCheckRunner(
            NullLogger<AvailabilityCheckRunner>.Instance,
            settings.Object,
            probe.Object,
            new AvailabilityCheckStatusStore(),
            notifications.Object,
            new AvailabilityCheckNotificationTracker());

        await runner.RunAsync(CancellationToken.None);
        await runner.RunAsync(CancellationToken.None);
        await runner.RunAsync(CancellationToken.None);

        notifications.Verify(
            n => n.NotifyUnreachableAsync("https://example.com/", "unreachable", It.IsAny<CancellationToken>()),
            Times.Once);
        notifications.Verify(
            n => n.NotifyRecoveredAsync("https://example.com/", "reachable", It.IsAny<CancellationToken>()),
            Times.Once);
        probe.Verify(
            p => p.ProbeAsync(
                "https://custom-probe.example/check.cgi",
                "https://example.com/",
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }
}
