using Microsoft.Extensions.Hosting;

namespace DataGateMonitor.Services.PiHoleHealth;

public sealed class PiHoleHealthBackgroundService(
    ILogger<PiHoleHealthBackgroundService> logger,
    IPiHoleHealthCheckRunner checkRunner) : BackgroundService
{
    private static readonly TimeSpan LoopDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!PiHoleHealthEnvironment.IsEnabled())
        {
            logger.LogInformation("{Service} is disabled via {Variable}",
                nameof(PiHoleHealthBackgroundService),
                PiHoleHealthEnvironment.DisabledVariable);
            return;
        }

        logger.LogInformation("{Service} started", nameof(PiHoleHealthBackgroundService));

        await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await checkRunner.RunAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(LoopDelay, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }

        logger.LogInformation("{Service} stopped", nameof(PiHoleHealthBackgroundService));
    }
}
