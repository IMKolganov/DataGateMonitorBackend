using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.Services.Others.Notifications.TrafficDaily;

namespace DataGateMonitor.Services.BackgroundServices;

public sealed class TrafficDailyRollupRunner(
    IServiceScopeFactory scopeFactory,
    ILogger<TrafficDailyRollupRunner> logger) : ITrafficDailyRollupRunner
{
    public async Task RunCatchUpThroughYesterdayAsync(CancellationToken ct)
    {
        var yesterday = TrafficDailyRollupPlanner.YesterdayUtc(DateTime.UtcNow);

        using var scope = scopeFactory.CreateScope();
        var rollup = scope.ServiceProvider.GetRequiredService<IOverviewTrafficDailyRollupService>();

        IReadOnlyList<DateOnly> missing;
        try
        {
            missing = await rollup.GetMissingRollupDaysAsync(yesterday, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to resolve missing traffic daily rollup days.");
            await NotifyFailedSafeAsync(scope, "missing-day lookup", ex, ct);
            throw;
        }

        if (missing.Count == 0)
        {
            logger.LogDebug("Traffic daily rollup is up to date through {Yesterday}.", yesterday);
            return;
        }

        logger.LogInformation(
            "Traffic daily rollup: {Count} missing UTC day(s) through {Yesterday}: {Days}.",
            missing.Count,
            yesterday,
            string.Join(", ", missing.Take(10)) + (missing.Count > 10 ? ", …" : string.Empty));

        var processed = new List<DateOnly>(missing.Count);
        var totalRows = 0;

        foreach (var day in missing)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var rows = await rollup.RollupDayAsync(day, ct);
                totalRows += rows;
                processed.Add(day);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Traffic daily rollup failed for UTC day {Day}.", day);
                var failure = new TrafficDailyRollupDayFailure(day, ex, processed, totalRows);
                await NotifyFailedSafeAsync(scope, failure, ct);
                throw;
            }
        }

        var result = new TrafficDailyRollupCatchUpResult(processed, totalRows);
        logger.LogInformation(
            "Traffic daily rollup completed: {DayCount} day(s), {Rows} session-day row(s).",
            result.ProcessedDays.Count,
            result.SessionDayRowsUpserted);

        await NotifySucceededSafeAsync(scope, result, ct);
    }

    private static async Task NotifySucceededSafeAsync(
        IServiceScope scope,
        TrafficDailyRollupCatchUpResult result,
        CancellationToken ct)
    {
        try
        {
            var notifier = scope.ServiceProvider.GetRequiredService<ITrafficDailyRollupNotificationService>();
            await notifier.NotifyCatchUpSucceededAsync(result, ct);
        }
        catch (Exception ex)
        {
            scope.ServiceProvider.GetRequiredService<ILogger<TrafficDailyRollupRunner>>()
                .LogWarning(ex, "Failed to send traffic daily rollup success notification.");
        }
    }

    private static async Task NotifyFailedSafeAsync(
        IServiceScope scope,
        TrafficDailyRollupDayFailure failure,
        CancellationToken ct)
    {
        try
        {
            var notifier = scope.ServiceProvider.GetRequiredService<ITrafficDailyRollupNotificationService>();
            await notifier.NotifyCatchUpFailedAsync(failure, ct);
        }
        catch (Exception ex)
        {
            scope.ServiceProvider.GetRequiredService<ILogger<TrafficDailyRollupRunner>>()
                .LogWarning(ex, "Failed to send traffic daily rollup failure notification.");
        }
    }

    private static async Task NotifyFailedSafeAsync(
        IServiceScope scope,
        string phase,
        Exception ex,
        CancellationToken ct)
    {
        try
        {
            var notifier = scope.ServiceProvider.GetRequiredService<ITrafficDailyRollupNotificationService>();
            await notifier.NotifyCatchUpFailedAsync(phase, $"{ex.GetType().Name}: {ex.Message}", ct);
        }
        catch (Exception notifyEx)
        {
            scope.ServiceProvider.GetRequiredService<ILogger<TrafficDailyRollupRunner>>()
                .LogWarning(notifyEx, "Failed to send traffic daily rollup failure notification.");
        }
    }
}
