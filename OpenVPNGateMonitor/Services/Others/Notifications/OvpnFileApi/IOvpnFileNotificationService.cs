namespace OpenVPNGateMonitor.Services.Others.Notifications.OvpnFileApi;


public interface IOvpnFileNotificationService
{
    Task NotifyReadByTokenAsync(string token, int fileId, int vpnServerId, bool isRevoked, CancellationToken ct);
    Task NotifyReadAllAsync(int vpnServerId, int count, CancellationToken ct);
    Task NotifyReadAllWithTokenAsync(int vpnServerId, int count, bool isRevoked, CancellationToken ct);

    Task NotifyReadByExternalIdAsync(string externalId, int count, CancellationToken ct);
    Task NotifyReadByExternalIdAndVpnServerIdAsync(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct);

    Task NotifyReadByExternalIdWithTokenAsync(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct);

    Task NotifyIssuedAsync(int vpnServerId, int fileId, string fileName, string externalId, /*todo: int actorUserId,*/
        CancellationToken ct);

    Task NotifyIssuedWithTokenAsync(int vpnServerId, int fileId, string fileName, string externalId,
        int tokenId, /*todo: int actorUserId,*/ CancellationToken ct);

    Task NotifyRevokedAsync(int vpnServerId, int fileId, string fileName, string externalId, /*todo: int actorUserId,*/
        CancellationToken ct);

    Task NotifyDownloadedAsync(int vpnServerId, string fileName, string externalId, /*todo: int actorUserId,*/
        bool isRevoked, CancellationToken ct);
}