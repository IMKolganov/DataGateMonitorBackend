using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Responses;

namespace DataGateMonitor.Services.RfAvailability;

public interface IRfAvailabilityCheckRunner
{
    Task<RfAvailabilityStatusResponse> RunAsync(CancellationToken ct, bool force = false);
}

public sealed class RfAvailabilityCheckRunner(
    ILogger<RfAvailabilityCheckRunner> logger,
    ISettingsService settingsService,
    IRfAvailabilityProbeClient probeClient,
    IRfAvailabilityStatusStore statusStore,
    IRfAvailabilityNotificationService notificationService,
    RfAvailabilityNotificationTracker notificationTracker) : IRfAvailabilityCheckRunner
{
    public async Task<RfAvailabilityStatusResponse> RunAsync(CancellationToken ct, bool force = false)
    {
        var (enabled, targetUrl) = await ResolveSettingsAsync(ct).ConfigureAwait(false);

        var snapshot = new RfAvailabilityStatusResponse
        {
            Enabled = enabled,
            TargetUrl = targetUrl,
            ProbeBaseUrl = RfAvailabilitySettingsKeys.DefaultProbeBaseUrl,
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

        logger.LogDebug("RF availability check: probing {TargetUrl}", targetUrl);

        var checkedAt = DateTimeOffset.UtcNow;
        var (result, error, durationMs) = await probeClient.ProbeAsync(targetUrl, ct).ConfigureAwait(false);

        snapshot.LastCheckedAtUtc = checkedAt;
        snapshot.LastDurationMs = durationMs;
        snapshot.LastError = error;
        snapshot.LastResult = result;

        statusStore.Save(snapshot);

        await NotifyIfNeededAsync(targetUrl, result, error, ct).ConfigureAwait(false);
        return snapshot;
    }

    private async Task<(bool Enabled, string TargetUrl)> ResolveSettingsAsync(CancellationToken ct)
    {
        // GetValueAsync<bool> returns false both when missing and when explicitly disabled.
        // TargetUrl is string — null means the feature was never configured → use defaults (enabled).
        var storedTarget = await settingsService
            .GetValueAsync<string>(RfAvailabilitySettingsKeys.TargetUrl, ct)
            .ConfigureAwait(false);

        if (storedTarget is null)
        {
            return (RfAvailabilitySettingsKeys.DefaultEnabled, RfAvailabilitySettingsKeys.DefaultTargetUrl);
        }

        var enabled = await settingsService
            .GetValueAsync<bool>(RfAvailabilitySettingsKeys.Enabled, ct)
            .ConfigureAwait(false);

        var targetUrl = string.IsNullOrWhiteSpace(storedTarget)
            ? RfAvailabilitySettingsKeys.DefaultTargetUrl
            : storedTarget.Trim();

        return (enabled, targetUrl);
    }

    private async Task NotifyIfNeededAsync(
        string targetUrl,
        RfAvailabilityProbeResultDto? result,
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
