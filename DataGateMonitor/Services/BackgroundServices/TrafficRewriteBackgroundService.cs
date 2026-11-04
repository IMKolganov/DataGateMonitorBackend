using Microsoft.Extensions.Options;

namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>
/// Drains <c>PendingTrafficRewrites</c> one job at a time (ORDER BY Id) under a PG advisory lock.
/// </summary>
public sealed class TrafficRewriteBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TrafficRewriteBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<ITrafficRewriteRunner>();
                await runner.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Traffic rewrite iteration failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
