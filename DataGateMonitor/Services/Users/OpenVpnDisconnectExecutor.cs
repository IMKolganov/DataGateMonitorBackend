using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

namespace DataGateMonitor.Services.Users;

public sealed class OpenVpnDisconnectExecutor(
    IOpenVpnClientService openVpnClientService,
    IIssuedOvpnFileQueryService issuedOvpnFileQueryService,
    IOvpnFileApiService ovpnFileApiService,
    ICommandService<FreeTierDisconnectLog, int> disconnectLogCommandService,
    ILogger<OpenVpnDisconnectExecutor> logger) : IOpenVpnDisconnectExecutor
{
    public async Task<KillOpenVpnClientResponse> ExecuteAsync(
        OpenVpnDisconnectRequest request, CancellationToken ct = default)
    {
        var (response, _) = await ExecuteWithLogIdAsync(request, ct);
        return response;
    }

    public async Task<(KillOpenVpnClientResponse Response, int? DisconnectLogId)> ExecuteWithLogIdAsync(
        OpenVpnDisconnectRequest request, CancellationToken ct = default)
    {
        var server = request.Server;
        var client = request.Client;

        var killSucceeded = true;
        string? errorMessage = null;

        try
        {
            await openVpnClientService.KillConnectedClientAsync(server, client, ct);
        }
        catch (Exception ex)
        {
            killSucceeded = false;
            errorMessage = ex.Message;
            logger.LogWarning(
                ex,
                "Failed to send OpenVPN kill for server {ServerId}, CN={CommonName}",
                server.Id,
                client.CommonName);
        }

        bool? revokeSucceeded = null;
        if (request.RevokeCertificate)
        {
            (revokeSucceeded, var revokeError) = await TryRevokeAsync(server, client.CommonName, ct);
            errorMessage = errorMessage is null
                ? revokeError
                : revokeError is null ? errorMessage : $"{errorMessage} | {revokeError}";
        }

        var logId = await WriteLogAsync(request, killSucceeded, revokeSucceeded, errorMessage, ct);

        var response = new KillOpenVpnClientResponse
        {
            Success = killSucceeded,
            RevokeAttempted = request.RevokeCertificate,
            RevokeSucceeded = revokeSucceeded,
            ErrorMessage = errorMessage,
        };

        return (response, logId);
    }

    public async Task UpdateNotificationOutcomeAsync(
        int disconnectLogId,
        string? notificationChannel,
        bool notificationSent,
        CancellationToken ct = default)
    {
        try
        {
            await disconnectLogCommandService.UpdateWhere(
                l => l.Id == disconnectLogId,
                set => set
                    .SetProperty(l => l.NotificationChannel, notificationChannel)
                    .SetProperty(l => l.NotificationSent, notificationSent),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to update notification outcome on free-tier disconnect log entry {DisconnectLogId}.",
                disconnectLogId);
        }
    }

    private async Task<(bool? Succeeded, string? Error)> TryRevokeAsync(
        VpnServer server, string? commonName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(commonName))
            return (false, "Cannot revoke: missing CommonName.");

        try
        {
            var issuedFile = await issuedOvpnFileQueryService.GetByCommonNameAndVpnServerIdAndIsRevoked(
                commonName, server.Id, isRevoked: false, ct);

            if (issuedFile is null)
            {
                return (false, $"No active issued OVPN file found for CN={commonName} on server {server.Id}.");
            }

            await ovpnFileApiService.RevokeOvpnFile(
                new RevokeFileRequest
                {
                    VpnServerId = server.Id,
                    OvpnFileId = issuedFile.Id,
                    CommonName = commonName,
                    IsRevoked = false,
                },
                ct);

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to revoke OVPN file for server {ServerId}, CN={CommonName}",
                server.Id,
                commonName);
            return (false, ex.Message);
        }
    }

    private async Task<int?> WriteLogAsync(
        OpenVpnDisconnectRequest request,
        bool killSucceeded,
        bool? revokeSucceeded,
        string? errorMessage,
        CancellationToken ct)
    {
        try
        {
            var entry = new FreeTierDisconnectLog
            {
                UserId = request.UserId,
                UserDisplayNameSnapshot = request.UserDisplayNameSnapshot,
                VpnServerId = request.Server.Id,
                VpnServerNameSnapshot = request.Server.ServerName,
                CommonName = request.Client.CommonName ?? string.Empty,
                ManagementClientId = request.Client.ManagementClientId,
                Reason = (int)request.Reason,
                InitiatedByUserId = request.InitiatedByUserId,
                RevokeRequested = request.RevokeCertificate,
                RevokeSucceeded = revokeSucceeded,
                KillSucceeded = killSucceeded,
                ErrorMessage = errorMessage,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var added = await disconnectLogCommandService.Add(entry, saveChanges: true, ct);
            return added.Id;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write free-tier disconnect log entry.");
            return null;
        }
    }
}
