using Mapster;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Hubs.Models;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Hubs.BackgroundService;

public sealed class OpenVpnStatusStreamPublisher : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IHubContext<OpenVpnStatusHub> hubContext;
    private readonly IOpenVpnBackgroundService openVpnBackgroundService;
    private readonly IOpenVpnServerOverviewQuery openVpnServerOverviewQuery;
    private readonly ILogger<OpenVpnStatusStreamPublisher> logger;

    private const string GroupName = "status-stream";

    public OpenVpnStatusStreamPublisher(
        IHubContext<OpenVpnStatusHub> hubContext,
        IOpenVpnBackgroundService openVpnBackgroundService,
        IOpenVpnServerOverviewQuery openVpnServerOverviewQuery,
        ILogger<OpenVpnStatusStreamPublisher> logger)
    {
        this.hubContext = hubContext;
        this.openVpnBackgroundService = openVpnBackgroundService;
        this.openVpnServerOverviewQuery = openVpnServerOverviewQuery;
        this.logger = logger;
    }

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

            await Task.Delay(1000, stoppingToken);
        }
    }
}