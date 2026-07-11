using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Users;

public sealed class FreeTierOpenVpnSessionEnforcementService(
    IVpnServerQueryService vpnServerQueryService,
    IOpenVpnClientService openVpnClientService,
    IIssuedOvpnFileQueryService issuedOvpnFileQueryService,
    IUserQueryService userQueryService,
    IFreeTierAccessComplianceService freeTierAccessComplianceService,
    IOpenVpnDisconnectExecutor disconnectExecutor,
    ISettingsService settingsService,
    ILogger<FreeTierOpenVpnSessionEnforcementService> logger) : IFreeTierOpenVpnSessionEnforcementService
{
    public async Task<bool> IsEnabledAsync(CancellationToken ct = default)
    {
        var typeKey = $"{FreeTierAccessSettingsKeys.EnforceOpenVpnSessions}_Type";
        var type = await settingsService.GetValueAsync<string>(typeKey, ct);
        if (!string.Equals(type, "bool", StringComparison.OrdinalIgnoreCase))
            return true;

        return await settingsService.GetValueAsync<bool>(
            FreeTierAccessSettingsKeys.EnforceOpenVpnSessions,
            ct);
    }

    public async Task<int> GetIntervalMinutesAsync(CancellationToken ct = default)
    {
        var typeKey = $"{FreeTierAccessSettingsKeys.EnforcementIntervalMinutes}_Type";
        var type = await settingsService.GetValueAsync<string>(typeKey, ct);
        if (!string.Equals(type, "int", StringComparison.OrdinalIgnoreCase))
            return 15;

        var minutes = await settingsService.GetValueAsync<int>(
            FreeTierAccessSettingsKeys.EnforcementIntervalMinutes,
            ct);

        return minutes > 0 ? minutes : 15;
    }

    public async Task<bool> IsRevokeOnEnforcementEnabledAsync(CancellationToken ct = default)
    {
        var typeKey = $"{FreeTierAccessSettingsKeys.RevokeOvpnOnEnforcement}_Type";
        var type = await settingsService.GetValueAsync<string>(typeKey, ct);
        if (!string.Equals(type, "bool", StringComparison.OrdinalIgnoreCase))
            return false;

        return await settingsService.GetValueAsync<bool>(
            FreeTierAccessSettingsKeys.RevokeOvpnOnEnforcement,
            ct);
    }

    public async Task<int> EnforceAsync(CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct))
        {
            logger.LogDebug("Free-tier OpenVPN session enforcement is disabled.");
            return 0;
        }

        var servers = (await vpnServerQueryService.GetAll(ct: ct))
            .Where(s => s.ServerType == VpnServerType.OpenVpn && !s.IsDisable && !s.IsDeleted)
            .ToList();

        var revokeOnEnforcement = await IsRevokeOnEnforcementEnabledAsync(ct);

        var killed = 0;
        foreach (var server in servers)
        {
            try
            {
                killed += await EnforceOnServerAsync(server, revokeOnEnforcement, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Free-tier OpenVPN enforcement failed for server {ServerId} ({ServerName})",
                    server.Id,
                    server.ServerName);
            }
        }

        if (killed > 0)
        {
            logger.LogInformation(
                "Free-tier OpenVPN enforcement disconnected {Count} non-compliant session(s).",
                killed);
        }

        return killed;
    }

    private async Task<int> EnforceOnServerAsync(VpnServer server, bool revokeOnEnforcement, CancellationToken ct)
    {
        var status = await openVpnClientService.GetClientsFromManagementAsync(server, ct);
        if (status.Clients.Count == 0)
            return 0;

        var killed = 0;
        var checkedUsers = new Dictionary<int, bool>();

        foreach (var connectedClient in status.Clients)
        {
            if (string.IsNullOrWhiteSpace(connectedClient.CommonName))
                continue;

            var externalId = await issuedOvpnFileQueryService.GetExternalIdByCommonName(
                connectedClient.CommonName,
                server.Id,
                ct);

            if (string.IsNullOrWhiteSpace(externalId))
                continue;

            var user = await userQueryService.GetByExternalId(externalId, ct);
            if (user is not { Id: > 0 })
                continue;

            if (!checkedUsers.TryGetValue(user.Id, out var shouldKill))
            {
                shouldKill = await freeTierAccessComplianceService.ShouldEnforceOpenVpnDisconnectAsync(user.Id, ct);
                checkedUsers[user.Id] = shouldKill;
            }

            if (!shouldKill)
                continue;

            var result = await disconnectExecutor.ExecuteAsync(
                new OpenVpnDisconnectRequest
                {
                    Server = server,
                    Client = connectedClient,
                    UserId = user.Id,
                    UserDisplayNameSnapshot = user.DisplayName ?? user.Email,
                    Reason = DisconnectReason.Enforcement,
                    InitiatedByUserId = null,
                    RevokeCertificate = revokeOnEnforcement,
                },
                ct);

            if (result.Success)
                killed++;

            logger.LogInformation(
                "Disconnected non-compliant Free/Default user {UserId} on server {ServerId}, CN={CommonName}, " +
                "ManagementClientId={ManagementClientId}, KillSucceeded={KillSucceeded}, RevokeAttempted={RevokeAttempted}, " +
                "RevokeSucceeded={RevokeSucceeded}",
                user.Id,
                server.Id,
                connectedClient.CommonName,
                connectedClient.ManagementClientId,
                result.Success,
                result.RevokeAttempted,
                result.RevokeSucceeded);
        }

        return killed;
    }
}
