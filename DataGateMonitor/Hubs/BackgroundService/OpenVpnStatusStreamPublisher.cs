using Mapster;
using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Hubs;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.Services.Cache;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.Hubs.BackgroundService;

public sealed class OpenVpnStatusStreamPublisher(
    IHubContext<OpenVpnStatusHub> hubContext,
    IOpenVpnBackgroundService openVpnBackgroundService,
    IServiceScopeFactory scopeFactory,
    IConnectedClientsCounterStore connectedClientsCounterStore,
    ILogger<OpenVpnStatusStreamPublisher> logger)
    : Microsoft.Extensions.Hosting.BackgroundService
{
    private const string GroupName = "status-stream";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var rawStatuses = openVpnBackgroundService.GetStatus();

                var statuses = rawStatuses.Values
                    .Select(x => x.Adapt<ServiceStatusResponse>())
                    .ToList();

                var serverIds = statuses
                    .Select(s => s.ServiceStatus.VpnServerId)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToArray();
                var connectedFromRedis = await connectedClientsCounterStore.GetManyAsync(serverIds, stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var openVpnServerOverviewQuery = scope.ServiceProvider.GetRequiredService<IVpnServerOverviewQuery>();

                foreach (var status in statuses)
                {
                    var vpnServerId = status.ServiceStatus.VpnServerId;

                    var (connectedClients, sessions) =
                        await openVpnServerOverviewQuery.GetClientCountersAsync(vpnServerId, stoppingToken);

                    status.ServiceStatus.CountConnectedClients =
                        connectedFromRedis.GetValueOrDefault(vpnServerId, connectedClients);
                    status.ServiceStatus.CountSessions = sessions;
                }

                var payload = new StatusStreamPayload
                {
                    Statuses = statuses,
                    TimestampUtc = DateTimeOffset.UtcNow
                };

                await hubContext.Clients.Group(GroupName)
                    .SendAsync("StatusUpdated", payload, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // normal
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Status stream publish error");
            }

            await Task.Delay(700, stoppingToken);//todo: move to settings
        }
    }
}