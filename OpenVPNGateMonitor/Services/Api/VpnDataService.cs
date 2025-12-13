using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;

namespace OpenVPNGateMonitor.Services.Api;

public class VpnDataService(
    ILogger<IVpnDataService> logger,
    IExternalIpAddressService externalIpAddressService,
    IOpenVpnServerQueryService openVpnServerQueryService,
    IOpenVpnServerOvpnFileConfigQueryService openVpnServerOvpnFileConfigQueryService,
    ITransactionRunner transactionRunner,
    ICommandService<OpenVpnServer, int> openVpnServerCommandService,
    ICommandService<OpenVpnServerOvpnFileConfig, int> openVpnServerOvpnFileConfigCommandService) : IVpnDataService
{
    public Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer server, CancellationToken ct)
        => transactionRunner.RunAsync(async _ =>
        {
            var now = DateTimeOffset.UtcNow;

            if (server.IsDefault)
            {
                // Unset previous default in one SQL statement (no entity loading)
                await openVpnServerCommandService.UpdateWhereAsync(
                    s => s.IsDefault,
                    u => u.SetProperty(x => x.IsDefault, false)
                        .SetProperty(x => x.LastUpdate, now),
                    ct);
            }

            // Insert server (need Id immediately for further operations)
            server.CreateDate = now;
            server.LastUpdate = now;
            await openVpnServerCommandService.AddAsync(server, saveChanges: true, ct);

            // Additional writes that must be part of the same transaction
            if (!await CheckAndPutDefaultExpiredSettings(server, ct))
                logger.LogWarning("Failed to add default settings for OpenVPN server.");

            // Return a fresh snapshot
            return await openVpnServerQueryService.GetByIdAsync(server.Id, ct)
                   ?? throw new InvalidOperationException("OpenVPN server not found");
        }, ct);


    public Task<OpenVpnServer> UpdateOpenVpnServer(OpenVpnServer server, CancellationToken ct)
        => transactionRunner.RunAsync(async _ =>
        {
            var now = DateTimeOffset.UtcNow;

            if (server.IsDefault)
            {
                // Unset all other defaults in a single SQL statement
                await openVpnServerCommandService.UpdateWhereAsync(
                    s => s.IsDefault && s.Id != server.Id,
                    u => u.SetProperty(x => x.IsDefault, false)
                        .SetProperty(x => x.LastUpdate, now),
                    ct);
            }

            // Update this server
            server.LastUpdate = now;
            await openVpnServerCommandService.UpdateAsync(server, saveChanges: true, ct);

            // Additional writes in the same transaction
            if (!await CheckAndPutDefaultExpiredSettings(server, ct))
                logger.LogWarning("Failed to add/update default settings for OpenVPN server.");

            // Return fresh snapshot
            return await openVpnServerQueryService.GetByIdAsync(server.Id, ct)
                   ?? throw new InvalidOperationException("OpenVPN server not found");
        }, ct);


    public async Task<bool> DeleteOpenVpnServer(int vpnServerId, CancellationToken ct)
    {
        var openVpnServer = await openVpnServerQueryService.GetByIdAsync(vpnServerId, ct)
                            ?? throw new InvalidOperationException("OpenVpnServer not found");
        await openVpnServerCommandService.DeleteAsync(openVpnServer, true, ct);
        return true;
    }

    private async Task<bool> CheckAndPutDefaultExpiredSettings(OpenVpnServer openVpnServer, CancellationToken ct)
    {
        var changesMade = false;

        if (!await openVpnServerOvpnFileConfigQueryService.AnyByVpnServerId(openVpnServer.Id, ct))
        {
            await openVpnServerOvpnFileConfigCommandService.AddAsync(new OpenVpnServerOvpnFileConfig
            {
                VpnServerId = openVpnServer.Id,
                VpnServerIp = await externalIpAddressService.GetRemoteIpAddress(ct),
            }, true, ct);
            changesMade = true;
        }

        return changesMade;
    }
}