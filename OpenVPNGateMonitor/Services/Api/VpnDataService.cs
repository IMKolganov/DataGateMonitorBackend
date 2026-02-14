using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;
using OpenVPNGateMonitor.Services.Others.Notifications.ServerOpenVpnApiClient;

namespace OpenVPNGateMonitor.Services.Api;

public class VpnDataService(
    ILogger<IVpnDataService> logger,
    IExternalIpAddressService externalIpAddressService,
    IOpenVpnServerQueryService openVpnServerQueryService,
    IOpenVpnServerOvpnFileConfigQueryService openVpnServerOvpnFileConfigQueryService,
    ITransactionRunner transactionRunner,
    ICommandService<OpenVpnServer, int> openVpnServerCommandService,
    ICommandService<OpenVpnServerOvpnFileConfig, int> openVpnServerOvpnFileConfigCommandService,
    ICommandService<QuotaPlanAllowedServer, int> quotaPlanAllowedServerCommandService,
    IServerOpenVpnNotificationService serverOpenVpnNotificationService) : IVpnDataService
{
    public async Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer server, List<int> quotaPlanIds, CancellationToken ct)
    {
        var result = await transactionRunner.RunAsync(async _ =>
        {
            var now = DateTimeOffset.UtcNow;

            if (server.IsDefault)
            {
                // Unset previous default in one SQL statement (no entity loading)
                await openVpnServerCommandService.UpdateWhere(
                    s => s.IsDefault,
                    u => u.SetProperty(x => x.IsDefault, false)
                        .SetProperty(x => x.LastUpdate, now),
                    ct);
            }

            // Insert server (need Id immediately for further operations)
            server.CreateDate = now;
            server.LastUpdate = now;
            await openVpnServerCommandService.Add(server, saveChanges: true, ct);

            await SyncQuotaPlanLinksAsync(server.Id, quotaPlanIds, ct);

            // Additional writes that must be part of the same transaction
            if (!await CheckAndPutDefaultExpiredSettings(server, ct))
                logger.LogWarning("Failed to add default settings for OpenVPN server.");

            // Return a fresh snapshot
            return await openVpnServerQueryService.GetById(server.Id, ct)
                   ?? throw new InvalidOperationException("OpenVPN server not found");
        }, ct);

        await serverOpenVpnNotificationService.NotifyAdded(result.Id, result.ServerName, ct);
        return result;
    }


    public async Task<OpenVpnServer> UpdateOpenVpnServer(OpenVpnServer server, List<int> quotaPlanIds, CancellationToken ct)
    {
        var result = await transactionRunner.RunAsync(async _ =>
        {
            var now = DateTimeOffset.UtcNow;

            if (server.IsDefault)
            {
                // Unset all other defaults in a single SQL statement
                await openVpnServerCommandService.UpdateWhere(
                    s => s.IsDefault && s.Id != server.Id,
                    u => u.SetProperty(x => x.IsDefault, false)
                        .SetProperty(x => x.LastUpdate, now),
                    ct);
            }

            // Update this server
            server.LastUpdate = now;
            await openVpnServerCommandService.Update(server, saveChanges: true, ct);

            await SyncQuotaPlanLinksAsync(server.Id, quotaPlanIds, ct);

            // Additional writes in the same transaction
            if (!await CheckAndPutDefaultExpiredSettings(server, ct))
                logger.LogWarning("Failed to add/update default settings for OpenVPN server.");

            // Return fresh snapshot
            return await openVpnServerQueryService.GetById(server.Id, ct)
                   ?? throw new InvalidOperationException("OpenVPN server not found");
        }, ct);

        await serverOpenVpnNotificationService.NotifyUpdated(result.Id, result.ServerName, ct);
        return result;
    }


    public async Task<bool> DeleteOpenVpnServer(int vpnServerId, CancellationToken ct)
    {
        var openVpnServer = await openVpnServerQueryService.GetById(vpnServerId, ct)
                            ?? throw new InvalidOperationException("OpenVpnServer not found");
        await openVpnServerCommandService.Delete(openVpnServer, true, ct);
        await serverOpenVpnNotificationService.NotifyDeleted(openVpnServer.Id, openVpnServer.ServerName, ct);
        return true;
    }

    private async Task<bool> CheckAndPutDefaultExpiredSettings(OpenVpnServer openVpnServer, CancellationToken ct)
    {
        var changesMade = false;

        if (!await openVpnServerOvpnFileConfigQueryService.AnyByVpnServerId(openVpnServer.Id, ct))
        {
            await openVpnServerOvpnFileConfigCommandService.Add(new OpenVpnServerOvpnFileConfig
            {
                VpnServerId = openVpnServer.Id,
                VpnServerIp = await externalIpAddressService.GetRemoteIpAddress(ct),
            }, true, ct);
            changesMade = true;
        }

        return changesMade;
    }
    
    private async Task SyncQuotaPlanLinksAsync(
        int vpnServerId,
        IReadOnlyCollection<int> quotaPlanIds,
        CancellationToken ct)
    {
        // Remove old links
        await quotaPlanAllowedServerCommandService.DeleteWhere(
            x => x.VpnServerId == vpnServerId,
            ct);

        // Add new links
        if (quotaPlanIds.Count == 0)
            return;

        var links = quotaPlanIds
            .Distinct()
            .Select(planId => new QuotaPlanAllowedServer
            {
                VpnServerId = vpnServerId,
                QuotaPlanId = planId
            })
            .ToList();

        await quotaPlanAllowedServerCommandService.AddRange(links, saveChanges: true, ct);
    }
}