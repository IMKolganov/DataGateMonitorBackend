using System.Security.Cryptography;
using System.Text;
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
    IssuedOvpnFileQueryService openVpnFileQueryService,
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
            var openVpnClients = await openVpnClientService.GetClientsAsync(openVpnServer, ct);
            logger.LogInformation(
                "VpnServerId: {Id}. Retrieved {Count} clients from OpenVPN.",
                openVpnServer.Id, openVpnClients.Count);

            await openVpnServerClientCommandService.UpdateWhereAsync(
                x => x.VpnServerId == openVpnServer.Id,
                s => s
                    .SetProperty(c => c.IsConnected, false)
                    .SetProperty(c => c.LastUpdate, DateTime.UtcNow),
                ct);

            foreach (var openVpnClient in openVpnClients)
            {
                var sessionId = GenerateSessionId(
                    openVpnClient.CommonName,
                    openVpnClient.RemoteIp,
                    openVpnClient.ConnectedSince);

                var existing = await openVpnServerClientQueryService
                    .GetBySessionAndServerIdAsync(sessionId, openVpnServer.Id, ct);

                if (existing != null)
                {
                    existing.CommonName = openVpnClient.CommonName;
                    existing.VpnServerId = openVpnServer.Id;
                    existing.ExternalId = await GetExternalIdByCommonNameFromOvpnFile(
                        openVpnClient.CommonName, openVpnServer.Id, ct) ?? string.Empty;
                    existing.BytesReceived = openVpnClient.BytesReceived;
                    existing.BytesSent = openVpnClient.BytesSent;
                    existing.LastUpdate = DateTime.UtcNow;
                    existing.Username = openVpnClient.Username;
                    existing.Country = openVpnClient.Country;
                    existing.Region = openVpnClient.Region;
                    existing.City = openVpnClient.City;
                    existing.Latitude = openVpnClient.Latitude;
                    existing.Longitude = openVpnClient.Longitude;
                    existing.IsConnected = true;

                    await openVpnServerClientCommandService.UpdateAsync(existing, saveChanges: false, ct);
                    logger.LogDebug("Updated client session {SessionId}.", sessionId);
                }
                else
                {
                    var newClient = new OpenVpnServerClient
                    {
                        VpnServerId = openVpnServer.Id,
                        ExternalId = await GetExternalIdByCommonNameFromOvpnFile(
                            openVpnClient.CommonName, openVpnServer.Id, ct) ?? string.Empty,
                        SessionId = sessionId,
                        CommonName = openVpnClient.CommonName,
                        RemoteIp = openVpnClient.RemoteIp,
                        LocalIp = openVpnClient.LocalIp,
                        BytesReceived = openVpnClient.BytesReceived,
                        BytesSent = openVpnClient.BytesSent,
                        ConnectedSince = openVpnClient.ConnectedSince,
                        Username = openVpnClient.Username,
                        Country = openVpnClient.Country,
                        Region = openVpnClient.Region,
                        City = openVpnClient.City,
                        Latitude = openVpnClient.Latitude,
                        Longitude = openVpnClient.Longitude,
                        IsConnected = true,
                        LastUpdate = DateTime.UtcNow,
                        CreateDate = DateTime.UtcNow
                    };

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
            if (serverInfo.OpenVpnState.UpSince <= DateTime.MinValue)
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
            openVpnServer.ServerName, // TODO: make server name
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
            existingStatusLog.LastUpdate = DateTime.UtcNow;

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
                LastUpdate = DateTime.UtcNow,
                CreateDate = DateTime.UtcNow
            };

            await openVpnServerStatusLogCommandService.AddAsync(newStatusLog, true, ct);
            logger.LogInformation($"VpnServerId: {openVpnServer.Id}. Created new status log {{SessionId}}.", sessionId);
        }
        logger.LogInformation($"VpnServerId: {openVpnServer.Id}. " +
                              $"SaveOpenVpnServerStatusLogAsync completed successfully.");
    }
    
    private Guid GenerateSessionId(string commonName, string realAddress, DateTime connectedSince)
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

    //todo: should be found where we need this method
    private async Task SetDisconnectForAllUsers(int vpnServerId, CancellationToken ct)
    {
        var existingAllOpenVpnServerClient = 
            await openVpnServerClientQueryService.GetAllConnectedByVpnServerIdAsync(vpnServerId, ct);

        foreach (var client in existingAllOpenVpnServerClient)
        {
            client.IsConnected = false;
        }
        logger.LogInformation($"VpnServerId: {vpnServerId}. " +
                               $"Marked {existingAllOpenVpnServerClient.Count} existing clients as disconnected.");
    }

    private async Task<string?> GetExternalIdByCommonNameFromOvpnFile(string commonName, int vpnServerId, 
        CancellationToken ct)
    {
        return await openVpnFileQueryService.GetExternalIdByCommonName(commonName, vpnServerId, false, ct);
    }

    // private async Task<string?> TryParseExternalIdAsync(string commonName, CancellationToken ct = default)
    // {
    //     var digitParts = Regex.Matches(commonName, @"\d+")
    //         .Select(m => m.Value)
    //         .Distinct();
    //     
    //     foreach (var candidate in digitParts)
    //     {
    //         if (!long.TryParse(candidate, out var id))
    //             continue;
    //     
    //         var exists = await telegramBotUserQueryService.AnyByTelegramIdAsync(id, ct);
    //     
    //         if (exists)
    //             return candidate;
    //     }
    //     
    //     return null;
    // }
}
