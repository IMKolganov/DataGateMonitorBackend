using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.VpnServerOvpnFileConfigTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.Helpers.Interfaces;
using DataGateMonitor.Services.Others.Notifications.ServerOpenVpnApiClient;

namespace DataGateMonitor.Services.Api;

public class VpnDataService(
    ILogger<IVpnDataService> logger,
    IExternalIpAddressService externalIpAddressService,
    IQuotaPlanQueryService quotaPlanQueryService,
    IVpnServerQueryService openVpnServerQueryService,
    IVpnServerOvpnFileConfigQueryService openVpnServerOvpnFileConfigQueryService,
    ITransactionRunner transactionRunner,
    ICommandService<VpnServer, int> openVpnServerCommandService,
    ICommandService<VpnServerOvpnFileConfig, int> openVpnServerOvpnFileConfigCommandService,
    ICommandService<QuotaPlanAllowedServer, int> quotaPlanAllowedServerCommandService,
    ICommandService<VpnServerTag, int> openVpnServerTagCommandService,
    IServerOpenVpnNotificationService serverOpenVpnNotificationService,
    IOpenVpnMicroserviceClientFactory microserviceClientFactory,
    IOpenVpnEventClientFactory eventClientFactory) : IVpnDataService
{
    public async Task<VpnServer> AddVpnServer(VpnServer server, List<int> quotaPlanIds, List<int> tagIds, CancellationToken ct)
    {
        var result = await transactionRunner.RunAsync(async _ =>
        {
            var now = DateTimeOffset.UtcNow;

            if (await openVpnServerQueryService.AnyByServerName(server.ServerName, ct))
            {
                logger.LogWarning("OpenVPN server with name '{ServerName}' already exists", server.ServerName);
                throw new InvalidOperationException("OpenVPN server with the same name already exists");
            }
            
            if (server.IsDefault)
            {
                // Unset the previous default in one SQL statement (no entity loading)
                await openVpnServerCommandService.UpdateWhere(
                    s => s.IsDefault,
                    u => u.SetProperty(x => x.IsDefault, false)
                        .SetProperty(x => x.LastUpdate, now),
                    ct);
            }

            // Insert server (need ID immediately for further operations)
            server.CreateDate = now;
            server.LastUpdate = now;
            await openVpnServerCommandService.Add(server, saveChanges: true, ct);

            var effectiveQuotaPlanIds = quotaPlanIds.Count > 0
                ? quotaPlanIds
                : (await quotaPlanQueryService.GetDefault(ct)) is { } defaultPlan
                    ? [defaultPlan.Id]
                    : [];

            await SyncQuotaPlanLinksAsync(server.Id, effectiveQuotaPlanIds, ct);
            await SyncTagLinksAsync(server.Id, tagIds, ct);

            // Additionally, writes that must be part of the same transaction
            if (!await CheckAndPutDefaultExpiredSettings(server, ct))
                logger.LogWarning("Failed to add default settings for OpenVPN server.");

            // Return a fresh snapshot
            return await openVpnServerQueryService.GetById(server.Id, ct)
                   ?? throw new InvalidOperationException("OpenVPN server not found");
        }, ct);

        await serverOpenVpnNotificationService.NotifyAdded(result.Id, result.ServerName, ct);
        return result;
    }


    public async Task<VpnServer> UpdateVpnServer(VpnServer server, List<int> quotaPlanIds, List<int> tagIds, CancellationToken ct)
    {
        var result = await transactionRunner.RunAsync(async _ =>
        {
            var now = DateTimeOffset.UtcNow;

            if (await openVpnServerQueryService.AnyByServerNameExceptId(server.ServerName, server.Id, ct))
            {
                logger.LogWarning("OpenVPN server with name '{ServerName}' already exists", server.ServerName);
                throw new InvalidOperationException("OpenVPN server with the same name already exists");
            }

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
            await SyncTagLinksAsync(server.Id, tagIds, ct);

            // Additional writes in the same transaction
            if (!await CheckAndPutDefaultExpiredSettings(server, ct))
                logger.LogWarning("Failed to add/update default settings for OpenVPN server.");

            // Return fresh snapshot
            return await openVpnServerQueryService.GetById(server.Id, ct)
                   ?? throw new InvalidOperationException("OpenVPN server not found");
        }, ct);

        await serverOpenVpnNotificationService.NotifyUpdated(result.Id, result.ServerName, ct);
        microserviceClientFactory.Invalidate(result.Id);
        eventClientFactory.Remove(result.Id);
        return result;
    }


    public async Task<bool> DeleteVpnServer(int vpnServerId, CancellationToken ct)
    {
        var openVpnServer = await openVpnServerQueryService.GetById(vpnServerId, ct)
                            ?? throw new InvalidOperationException("VpnServer not found");
        var now = DateTimeOffset.UtcNow;
        await openVpnServerCommandService.UpdateWhere(
            x => x.Id == vpnServerId,
            u => u.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.LastUpdate, now),
            ct);
        await serverOpenVpnNotificationService.NotifyDeleted(openVpnServer.Id, openVpnServer.ServerName, ct);
        microserviceClientFactory.Invalidate(openVpnServer.Id);
        eventClientFactory.Remove(openVpnServer.Id);
        return true;
    }

    private const string DefaultXrayClientLinkTemplate =
        "{{vless_uri}}\r\n# {{friendly_name}}\r\nUUID: {{uuid}}\r\nEndpoint: {{server_ip}}:{{server_port}}\r\n";

    private async Task<bool> CheckAndPutDefaultExpiredSettings(VpnServer openVpnServer, CancellationToken ct)
    {
        if (openVpnServer.ServerType == VpnServerType.OpenVpn)
            return await EnsureOpenVpnDefaultExportConfigAsync(openVpnServer, ct);

        if (openVpnServer.ServerType == VpnServerType.Xray)
            return await EnsureXrayDefaultExportConfigAsync(openVpnServer, ct);

        return false;
    }

    private async Task<bool> EnsureOpenVpnDefaultExportConfigAsync(VpnServer server, CancellationToken ct)
    {
        if (await openVpnServerOvpnFileConfigQueryService.AnyByVpnServerId(server.Id, ct))
            return false;

        await openVpnServerOvpnFileConfigCommandService.Add(new VpnServerOvpnFileConfig
        {
            VpnServerId = server.Id,
            VpnServerIp = await externalIpAddressService.GetRemoteIpAddress(ct),
        }, true, ct);
        return true;
    }

    private async Task<bool> EnsureXrayDefaultExportConfigAsync(VpnServer server, CancellationToken ct)
    {
        if (await openVpnServerOvpnFileConfigQueryService.AnyByVpnServerId(server.Id, ct))
            return false;

        var ip = await externalIpAddressService.GetRemoteIpAddress(ct);
        await openVpnServerOvpnFileConfigCommandService.Add(new VpnServerOvpnFileConfig
        {
            VpnServerId = server.Id,
            VpnServerIp = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip,
            VpnServerPort = 443,
            ConfigTemplate = DefaultXrayClientLinkTemplate,
        }, true, ct);
        return true;
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

    private async Task SyncTagLinksAsync(
        int vpnServerId,
        IReadOnlyCollection<int> tagIds,
        CancellationToken ct)
    {
        await openVpnServerTagCommandService.DeleteWhere(
            x => x.VpnServerId == vpnServerId,
            ct);

        if (tagIds.Count == 0)
            return;

        var links = tagIds
            .Distinct()
            .Select(tagId => new VpnServerTag
            {
                VpnServerId = vpnServerId,
                TagId = tagId
            })
            .ToList();

        await openVpnServerTagCommandService.AddRange(links, saveChanges: true, ct);
    }
}