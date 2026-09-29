using Microsoft.Extensions.Hosting;

namespace DataGateMonitor.Services.RfAvailability;

public sealed class RfAvailabilityBackgroundService(
    ILogger<RfAvailabilityBackgroundService> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan LoopDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!RfAvailabilityEnvironment.IsEnabled())
        {
            logger.LogInformation("{Service} is disabled via {Variable}",
                nameof(RfAvailabilityBackgroundService),
                RfAvailabilityEnvironment.DisabledVariable);
            return;
        }

        logger.LogInformation("{Service} started", nameof(RfAvailabilityBackgroundService));

        await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var runner = scope.ServiceProvider.GetRequiredService<IRfAvailabilityCheckRunner>();
                    await runner.RunAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Service} iteration failed", nameof(RfAvailabilityBackgroundService));
                }

                await Task.Delay(LoopDelay, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }

        logger.LogInformation("{Service} stopped", nameof(RfAvailabilityBackgroundService));
    }
}
