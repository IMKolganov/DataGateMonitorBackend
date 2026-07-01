namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>
/// Builds daily traffic rollups for completed UTC days (default: catch up through yesterday).
/// Optional one-time backfill: set TRAFFIC_DAILY_BACKFILL_ON_START=true.
/// </summary>
public sealed class TrafficDailyRollupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TrafficDailyRollupBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TrafficDailyRollupEnvironment.IsEnabled())
        {
            logger.LogInformation("Traffic daily rollup background service is disabled.");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        if (TrafficDailyRollupEnvironment.IsBackfillOnStartEnabled())
        {
            logger.LogInformation("TRAFFIC_DAILY_BACKFILL_ON_START enabled — starting historical catch-up.");
            await RunCatchUpSafeAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCatchUpSafeAsync(stoppingToken);
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RunCatchUpSafeAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<ITrafficDailyRollupRunner>();
            await runner.RunCatchUpThroughYesterdayAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Traffic daily rollup iteration failed.");
        }
    }
}
