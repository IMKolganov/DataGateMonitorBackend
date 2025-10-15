using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Services.DataGateCertManager.Events;

namespace OpenVPNGateMonitor.Services.BackgroundServices;

public sealed class OpenVpnEventBackgroundService(
    ILogger<OpenVpnEventBackgroundService> logger,
    IOpenVpnEventClientFactory eventClientFactory,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    private static int _instanceCount = 0;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var newInstanceCount = Interlocked.Increment(ref _instanceCount);
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

            // Keep service alive until host is stopping
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
            logger.LogInformation("OpenVpnEventBackgroundService stopping...");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled error in OpenVpnEventBackgroundService");
            throw;
        }
    }
}