using System.Security.Cryptography;
using System.Text;
using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerStatusLogTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;

namespace OpenVPNGateMonitor.Services.BackgroundServices;

public class OpenVpnServerService(
    ILogger<IOpenVpnServerService> logger,
    IOpenVpnClientService openVpnClientService,
    IOpenVpnSummaryStatService openVpnSummaryStatService,
    IOpenVpnVersionService openVpnVersionService,
    IOpenVpnStateService openVpnStateService,
    IOpenVpnServerClientQueryService openVpnServerClientQueryService,
    IIssuedOvpnFileQueryService openVpnFileQueryService,
    IOpenVpnServerStatusLogQueryService openVpnServerStatusLogQueryService,
    IExternalIpAddressService externalIpAddressService,
    ITransactionRunner transactionRunner,
    ICommandService<OpenVpnServerClient, int> openVpnServerClientCommandService,
    ICommandService<OpenVpnServerStatusLog, int> openVpnServerStatusLogCommandService) : IOpenVpnServerService
{
    public async Task SaveConnectedClientsAsync(OpenVpnServer openVpnServer, CancellationToken ct)
    {
        logger.LogInformation(
            "VpnServerId: {Id}. Starting SaveConnectedClientsAsync...",
            openVpnServer.Id);

        await transactionRunner.RunAsync(async _ =>
        {
            var openVpnClientsFromMng = 
                await openVpnClientService.GetClientsFromManagementAsync(openVpnServer, ct);
            logger.LogInformation(
                "VpnServerId: {Id}. Retrieved {Count} clients from OpenVPN.",
                openVpnServer.Id, openVpnClientsFromMng.Count);
            
            var nowUtc = DateTimeOffset.UtcNow;
            var currentSessionIds = new HashSet<Guid>(
                openVpnClientsFromMng.Select(c => 
                    GenerateSessionId(c.CommonName, c.RemoteIp, c.ConnectedSince)));

            await openVpnServerClientCommandService.UpdateWhereAsync(
                x => x.VpnServerId == openVpnServer.Id
                     && x.IsConnected
                     && !currentSessionIds.Contains(x.SessionId),
                s => s
                    .SetProperty(c => c.IsConnected, false)
                    .SetProperty(c => c.DisconnectedAt, nowUtc)
                    .SetProperty(c => c.LastUpdate, nowUtc),
                ct);

            foreach (var openVpnClientFromMng in openVpnClientsFromMng)
            {
                var sessionId = GenerateSessionId(
                    openVpnClientFromMng.CommonName,
                    openVpnClientFromMng.RemoteIp,
                    openVpnClientFromMng.ConnectedSince);

                var existing = await openVpnServerClientQueryService
                    .GetBySessionAndServerIdAsync(sessionId, openVpnServer.Id, ct);

                if (existing != null)
                {
                    openVpnClientFromMng.Adapt(existing);

                    existing.VpnServerId = openVpnServer.Id;
                    existing.ExternalId = await openVpnFileQueryService.GetExternalIdByCommonName(
                        openVpnClientFromMng.CommonName, openVpnServer.Id, false, ct) ?? string.Empty;
                    existing.LastUpdate = DateTimeOffset.UtcNow;
                    existing.IsConnected = true;
                    existing.DisconnectedAt = null;

                    await openVpnServerClientCommandService.UpdateAsync(existing, saveChanges: false, ct);
                    logger.LogDebug("Updated client session {SessionId}.", sessionId);
                }
                else
                {
                    var newClient = openVpnClientFromMng.Adapt<OpenVpnServerClient>();

                    newClient.VpnServerId = openVpnServer.Id;
                    newClient.SessionId = sessionId;
                    newClient.ExternalId = await openVpnFileQueryService.GetExternalIdByCommonName(
                        openVpnClientFromMng.CommonName, openVpnServer.Id, false, ct) ?? string.Empty;
                    newClient.DisconnectedAt = null;
                    newClient.IsConnected = true;
                    newClient.LastUpdate = DateTimeOffset.UtcNow;
                    newClient.CreateDate = DateTimeOffset.UtcNow;

                    await openVpnServerClientCommandService.AddAsync(newClient, saveChanges: false, ct);
                    logger.LogInformation(
                        "VpnServerId: {Id}. Added new client session {SessionId}.",
                        openVpnServer.Id, sessionId);
                }
            }

            await openVpnServerClientCommandService.SaveChangesAsync(ct);

            logger.LogInformation(
                "VpnServerId: {Id}. SaveConnectedClientsAsync completed successfully.",
                openVpnServer.Id);

        }, ct);
    }

    public async Task SaveOpenVpnServerStatusLogAsync(OpenVpnServer openVpnServer, CancellationToken ct)
    {
        logger.LogInformation($"VpnServerId: {openVpnServer.Id}. Starting SaveOpenVpnServerStatusLogAsync...");

        var serverInfo = new ServerInfo();
        try
        {
            serverInfo.OpenVpnState = await openVpnStateService.GetStateAsync(openVpnServer, ct);
            if (serverInfo.OpenVpnState.UpSince <= DateTimeOffset.MinValue)
            {
                throw new Exception($"VpnServerId: {openVpnServer.Id}. UpSince is not set. " +
                                    $"Check your configuration or server.");
            }
            
            serverInfo.OpenVpnSummaryStats = await openVpnSummaryStatService.GetSummaryStatsAsync(openVpnServer, 
                ct);
            serverInfo.OpenVpnState.ServerRemoteIp = await externalIpAddressService.GetRemoteIpAddress(ct);

            if (serverInfo.OpenVpnState != null)
            {
                serverInfo.Version = await openVpnVersionService.GetVersionAsync(openVpnServer, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"VpnServerId: {openVpnServer.Id}. Failed to get OpenVPN Summary Stats. " +
                                 $"Error: {ex.Message}");
            throw;
        }

        serverInfo.Status = serverInfo.OpenVpnState != null ? "CONNECTED" : "DISCONNECTED";


        if (serverInfo.OpenVpnState == null)
        {
            logger.LogWarning($"VpnServerId: {openVpnServer.Id}. OpenVPN State is null. Cannot proceed.");
            throw new InvalidOperationException($"VpnServerId: {openVpnServer.Id}. OpenVPN State is null. Cannot proceed.");
        }

        var sessionId = GenerateSessionId(
            $"{openVpnServer.Id}{openVpnServer.ServerName}",
            serverInfo.OpenVpnState.ServerLocalIp ?? 
            throw new InvalidOperationException($"VpnServerId: {openVpnServer.Id}. LocalIp cannot be null"),
            serverInfo.OpenVpnState.UpSince
        );

        var existingStatusLog =
            await openVpnServerStatusLogQueryService.GetBySessionIdAndVpnServerIdAsync(sessionId, openVpnServer.Id, ct);

        if (existingStatusLog != null)
        {
            existingStatusLog.VpnServerId = openVpnServer.Id;
            existingStatusLog.UpSince = serverInfo.OpenVpnState.UpSince;
            existingStatusLog.ServerLocalIp = serverInfo.OpenVpnState.ServerLocalIp;
            existingStatusLog.ServerRemoteIp = serverInfo.OpenVpnState.ServerRemoteIp;
            existingStatusLog.BytesIn = serverInfo.OpenVpnSummaryStats?.BytesIn ?? 0;
            existingStatusLog.BytesOut = serverInfo.OpenVpnSummaryStats?.BytesOut ?? 0;
            existingStatusLog.Version = serverInfo.Version;
            existingStatusLog.LastUpdate = DateTimeOffset.UtcNow;

            await openVpnServerStatusLogCommandService.UpdateAsync(existingStatusLog, true, ct);
            logger.LogInformation($"VpnServerId: {openVpnServer.Id}. Updated existing status log {sessionId}.");
        }
        else
        {
            var newStatusLog = new OpenVpnServerStatusLog
            {
                VpnServerId = openVpnServer.Id,
                SessionId = sessionId,
                UpSince = serverInfo.OpenVpnState.UpSince,
                ServerLocalIp = serverInfo.OpenVpnState.ServerLocalIp,
                ServerRemoteIp = serverInfo.OpenVpnState.ServerRemoteIp,
                BytesIn = serverInfo.OpenVpnSummaryStats?.BytesIn ?? 0,
                BytesOut = serverInfo.OpenVpnSummaryStats?.BytesOut ?? 0,
                Version = serverInfo.Version,
                LastUpdate = DateTimeOffset.UtcNow,
                CreateDate = DateTimeOffset.UtcNow
            };

            await openVpnServerStatusLogCommandService.AddAsync(newStatusLog, true, ct);
            logger.LogInformation($"VpnServerId: {openVpnServer.Id}. Created new status log {{SessionId}}.", sessionId);
        }
        logger.LogInformation($"VpnServerId: {openVpnServer.Id}. " +
                              $"SaveOpenVpnServerStatusLogAsync completed successfully.");
    }
    
    private Guid GenerateSessionId(string commonName, string realAddress, DateTimeOffset connectedSince)
    {
        logger.LogDebug($"Generating SessionId for CommonName: {commonName}, RealAddress: {realAddress}," +
                         $" ConnectedSince: {connectedSince}");

        var sessionString = $"{commonName}-{realAddress}-{connectedSince:o}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sessionString));
    
        var sessionId = new Guid(hashBytes.Take(16).ToArray());
        logger.LogDebug($"Generated SessionId: {sessionId}");

        return sessionId;
    }
}
