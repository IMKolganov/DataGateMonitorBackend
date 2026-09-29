using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses;

namespace DataGateMonitor.Services.AvailabilityCheck;

public interface IAvailabilityCheckRunner
{
    Task<AvailabilityCheckStatusResponse> RunAsync(CancellationToken ct, bool force = false);
}

public sealed class AvailabilityCheckRunner(
    ILogger<AvailabilityCheckRunner> logger,
    ISettingsService settingsService,
    IAvailabilityCheckProbeClient probeClient,
    IAvailabilityCheckStatusStore statusStore,
    IAvailabilityCheckNotificationService notificationService,
    AvailabilityCheckNotificationTracker notificationTracker) : IAvailabilityCheckRunner
{
    public async Task<AvailabilityCheckStatusResponse> RunAsync(CancellationToken ct, bool force = false)
    {
        var (enabled, targetUrl, probeUrl) = await ResolveSettingsAsync(ct).ConfigureAwait(false);

        var snapshot = new AvailabilityCheckStatusResponse
        {
            Enabled = enabled,
            TargetUrl = targetUrl,
            ProbeUrl = probeUrl,
        };

        if (!enabled && !force)
        {
            var previous = statusStore.Get();
            if (previous is not null)
            {
                snapshot.LastCheckedAtUtc = previous.LastCheckedAtUtc;
                snapshot.LastDurationMs = previous.LastDurationMs;
                snapshot.LastError = previous.LastError;
                snapshot.LastResult = previous.LastResult;
            }

            statusStore.Save(snapshot);
            return snapshot;
        }

        logger.LogDebug("Availability check: probing {TargetUrl} via {ProbeUrl}", targetUrl, probeUrl);

        var checkedAt = DateTimeOffset.UtcNow;
        var (result, error, durationMs) = await probeClient
            .ProbeAsync(probeUrl, targetUrl, ct)
            .ConfigureAwait(false);

        snapshot.LastCheckedAtUtc = checkedAt;
        snapshot.LastDurationMs = durationMs;
        snapshot.LastError = error;
        snapshot.LastResult = result;

        statusStore.Save(snapshot);

        await NotifyIfNeededAsync(targetUrl, result, error, ct).ConfigureAwait(false);
        return snapshot;
    }

    private async Task<(bool Enabled, string TargetUrl, string ProbeUrl)> ResolveSettingsAsync(CancellationToken ct)
    {
        // TargetUrl null ⇒ never configured ⇒ defaults (enabled + default URLs).
        var storedTarget = await settingsService
            .GetValueAsync<string>(AvailabilityCheckSettingsKeys.TargetUrl, ct)
            .ConfigureAwait(false);

        var storedProbe = await settingsService
            .GetValueAsync<string>(AvailabilityCheckSettingsKeys.ProbeUrl, ct)
            .ConfigureAwait(false);

        var probeUrl = string.IsNullOrWhiteSpace(storedProbe)
            ? AvailabilityCheckSettingsKeys.DefaultProbeUrl
            : storedProbe.Trim();

        if (storedTarget is null)
        {
            return (
                AvailabilityCheckSettingsKeys.DefaultEnabled,
                AvailabilityCheckSettingsKeys.DefaultTargetUrl,
                probeUrl);
        }

        var enabled = await settingsService
            .GetValueAsync<bool>(AvailabilityCheckSettingsKeys.Enabled, ct)
            .ConfigureAwait(false);

        var targetUrl = string.IsNullOrWhiteSpace(storedTarget)
            ? AvailabilityCheckSettingsKeys.DefaultTargetUrl
            : storedTarget.Trim();

        return (enabled, targetUrl, probeUrl);
    }

    private async Task NotifyIfNeededAsync(
        string targetUrl,
        AvailabilityProbeResultDto? result,
        string? error,
        CancellationToken ct)
    {
        var reachable = result?.Reachable == true && string.IsNullOrWhiteSpace(error);
        var summary = !string.IsNullOrWhiteSpace(error)
            ? error!
            : result?.Summary ?? (reachable ? "reachable" : "unreachable");

        if (reachable)
        {
            if (notificationTracker.HasUnreachableNotification(targetUrl)
                && notificationTracker.TryMarkRecoveredNotified(targetUrl))
            {
                await notificationService.NotifyRecoveredAsync(targetUrl, summary, ct).ConfigureAwait(false);
            }

            notificationTracker.ClearUnreachable(targetUrl);
            return;
        }

        if (!notificationTracker.TryMarkUnreachableNotified(targetUrl, summary))
            return;

        await notificationService.NotifyUnreachableAsync(targetUrl, summary, ct).ConfigureAwait(false);
    }
}
