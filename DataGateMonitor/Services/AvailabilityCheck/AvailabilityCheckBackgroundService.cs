using DataGateMonitor.Services.Others;
using Microsoft.Extensions.Hosting;

namespace DataGateMonitor.Services.AvailabilityCheck;

public sealed class AvailabilityCheckBackgroundService(
    ILogger<AvailabilityCheckBackgroundService> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    /// <summary>Overridable for unit tests (default 30s).</summary>
    internal static TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!AvailabilityCheckEnvironment.IsEnabled())
        {
            logger.LogInformation("{Service} is disabled via {Variable}",
                nameof(AvailabilityCheckBackgroundService),
                AvailabilityCheckEnvironment.DisabledVariable);
            return;
        }

        logger.LogInformation("{Service} started", nameof(AvailabilityCheckBackgroundService));

        await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var runner = scope.ServiceProvider.GetRequiredService<IAvailabilityCheckRunner>();
                    await runner.RunAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Service} iteration failed", nameof(AvailabilityCheckBackgroundService));
                }

                var delay = await ResolveLoopDelayAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }

        logger.LogInformation("{Service} stopped", nameof(AvailabilityCheckBackgroundService));
    }

    private async Task<TimeSpan> ResolveLoopDelayAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var stored = await settings
                .GetValueAsync<int>(AvailabilityCheckSettingsKeys.IntervalSeconds, ct)
                .ConfigureAwait(false);

            // Missing key → GetValueAsync<int> returns 0; treat as default.
            var seconds = stored > 0
                ? AvailabilityCheckSettingsKeys.ClampIntervalSeconds(stored)
                : AvailabilityCheckSettingsKeys.DefaultIntervalSeconds;

            return TimeSpan.FromSeconds(seconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "{Service}: failed to read interval; using default {Seconds}s",
                nameof(AvailabilityCheckBackgroundService),
                AvailabilityCheckSettingsKeys.DefaultIntervalSeconds);
            return TimeSpan.FromSeconds(AvailabilityCheckSettingsKeys.DefaultIntervalSeconds);
        }
    }
}
