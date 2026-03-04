using Mapster;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Hubs.Models;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Hubs.BackgroundService;

public sealed class OpenVpnStatusStreamPublisher(
    IHubContext<OpenVpnStatusHub> hubContext,
    IOpenVpnBackgroundService openVpnBackgroundService,
    IServiceScopeFactory scopeFactory,
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

                using var scope = scopeFactory.CreateScope();
                var openVpnServerOverviewQuery = scope.ServiceProvider.GetRequiredService<IOpenVpnServerOverviewQuery>();

                foreach (var status in statuses)
                {
                    var vpnServerId = status.ServiceStatus.VpnServerId;

                    var (connectedClients, sessions) =
                        await openVpnServerOverviewQuery.GetClientCountersAsync(vpnServerId, stoppingToken);

                    status.ServiceStatus.CountConnectedClients = connectedClients;
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