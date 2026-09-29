using Microsoft.Extensions.Hosting;

namespace DataGateMonitor.Services.AvailabilityCheck;

public sealed class AvailabilityCheckBackgroundService(
    ILogger<AvailabilityCheckBackgroundService> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    /// <summary>Overridable for unit tests (defaults: 30s startup, 5m loop).</summary>
    internal static TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Overridable for unit tests.</summary>
    internal static TimeSpan LoopDelay { get; set; } = TimeSpan.FromMinutes(5);

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

                await Task.Delay(LoopDelay, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }

        logger.LogInformation("{Service} stopped", nameof(AvailabilityCheckBackgroundService));
    }
}
