using Microsoft.Extensions.Hosting;

namespace OpenVPNGateMonitor.Services.GeoLite;

/// <summary>
/// Periodically runs <see cref="IGeoLiteScheduledUpdateRunner"/> using
/// <see cref="GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays"/> from settings (days since local DB file write time; 0 = disabled).
/// </summary>
public sealed class GeoLiteAutoUpdateBackgroundService(
    ILogger<GeoLiteAutoUpdateBackgroundService> logger,
    IGeoLiteScheduledUpdateRunner scheduledUpdateRunner)
    : BackgroundService
{
    private static readonly TimeSpan LoopDelay = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Service} started", nameof(GeoLiteAutoUpdateBackgroundService));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await scheduledUpdateRunner.RunAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(LoopDelay, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }

        logger.LogInformation("{Service} stopped", nameof(GeoLiteAutoUpdateBackgroundService));
    }
}
