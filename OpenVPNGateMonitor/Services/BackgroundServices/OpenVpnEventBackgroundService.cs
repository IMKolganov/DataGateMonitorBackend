using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager.Events;

namespace OpenVPNGateMonitor.Services.BackgroundServices;

public class OpenVpnEventBackgroundService(
    ILogger<OpenVpnEventBackgroundService> logger,
    IOpenVpnEventClientFactory eventClientFactory,
    IServiceScopeFactory scopeFactory)
    : BackgroundService, IOpenVpnEventBackgroundService
{
    private static int _instanceCount = 0;
    private CancellationTokenSource _delayTokenSource = new();

    public async Task RunNow(CancellationToken cancellationToken)
    {
        logger.LogInformation("Manual trigger received. Cancelling wait...");

        if (!_delayTokenSource.IsCancellationRequested)
            await _delayTokenSource.CancelAsync();

        _delayTokenSource.Dispose();
        _delayTokenSource = new CancellationTokenSource();
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        int newInstanceCount = Interlocked.Increment(ref _instanceCount);

        if (newInstanceCount > 1)
        {
            logger.LogCritical("Multiple instances detected! Total instances: {NewInstanceCount}", newInstanceCount);
            throw new InvalidOperationException("Only one instance of OpenVpnEventBackgroundService is allowed.");
        }

        logger.LogInformation("OpenVpnEventBackgroundService started");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var openVpnOverviewQuery = scope.ServiceProvider.GetRequiredService<IOpenVpnServerQueryService>();
            var servers = await openVpnOverviewQuery.GetAllAsync(cancellationToken);


            foreach (var server in servers)
            {
                try
                {
                    var client = eventClientFactory.Create(server);
                    await client.StartListeningAsync(cancellationToken);
                    logger.LogInformation("Started listening to events from OpenVPN server {ServerId}", server.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start listening to server {ServerId}", server.Id);
                }
            }

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled error in OpenVpnEventBackgroundService");
            throw;
        }
    }
}
