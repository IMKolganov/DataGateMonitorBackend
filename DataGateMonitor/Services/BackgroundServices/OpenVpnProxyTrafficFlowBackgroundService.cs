using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.BackgroundServices;

public sealed class OpenVpnProxyTrafficFlowBackgroundService(
    ILogger<OpenVpnProxyTrafficFlowBackgroundService> logger,
    IOpenVpnProxyTrafficFlowClientFactory trafficFlowClientFactory,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("OpenVpnProxyTrafficFlowBackgroundService started");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = scopeFactory.CreateScope();
                var openVpnOverviewQuery = scope.ServiceProvider.GetRequiredService<IVpnServerQueryService>();
                var servers = await openVpnOverviewQuery.GetAll(ct: cancellationToken);
                servers = servers
                    .Where(x => !x.IsDisable && x.ServerType == VpnServerType.OpenVpn)
                    .ToList();

                foreach (var server in servers)
                {
                    try
                    {
                        var client = trafficFlowClientFactory.Create(server);
                        await client.StartListeningAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to start proxy traffic flow listener for server {ServerId}", server.Id);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("OpenVpnProxyTrafficFlowBackgroundService stopping...");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled error in OpenVpnProxyTrafficFlowBackgroundService");
            throw;
        }
    }
}
