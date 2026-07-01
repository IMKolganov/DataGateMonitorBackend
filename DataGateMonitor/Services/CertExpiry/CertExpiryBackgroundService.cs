using Microsoft.Extensions.Hosting;

namespace DataGateMonitor.Services.CertExpiry;

/// <summary>
/// Polls OpenVPN nodes for client certificate expiry and notifies admins when issued profiles need renewal.
/// Join key: <see cref="Models.IssuedOvpnFile.CommonName"/> ↔ PKI <c>index.txt</c> (serial in <c>CertId</c> is not persisted today).
/// </summary>
public sealed class CertExpiryBackgroundService(
    ILogger<CertExpiryBackgroundService> logger,
    ICertExpiryScheduledCheckRunner scheduledCheckRunner) : BackgroundService
{
    private static readonly TimeSpan LoopDelay = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!CertExpiryEnvironment.IsEnabled())
        {
            logger.LogInformation("{Service} is disabled via {Variable}",
                nameof(CertExpiryBackgroundService),
                CertExpiryEnvironment.DisabledVariable);
            return;
        }

        logger.LogInformation("{Service} started", nameof(CertExpiryBackgroundService));

        await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await scheduledCheckRunner.RunAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(LoopDelay, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }

        logger.LogInformation("{Service} stopped", nameof(CertExpiryBackgroundService));
    }
}
