namespace OpenVPNGateMonitor.Services.Others.Notifications.OvpnFileApi;


public interface IOvpnFileNotificationService
{
    Task NotifyReadByToken(string token, int fileId, int vpnServerId, bool isRevoked, CancellationToken ct);
    Task NotifyReadAll(int vpnServerId, int count, CancellationToken ct);
    Task NotifyReadAllWithToken(int vpnServerId, int count, bool isRevoked, CancellationToken ct);

    Task NotifyReadByExternalId(string externalId, int count, CancellationToken ct);
    Task NotifyReadByExternalIdAndVpnServerId(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct);

    Task NotifyReadByExternalIdWithToken(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct);

    Task NotifyIssued(int vpnServerId, int fileId, string fileName, string externalId, /*todo: int actorUserId,*/
        CancellationToken ct);

    Task NotifyIssuedWithToken(int vpnServerId, int fileId, string fileName, string externalId,
        int tokenId, /*todo: int actorUserId,*/ CancellationToken ct);

    Task NotifyRevoked(int vpnServerId, int fileId, string fileName, string externalId, /*todo: int actorUserId,*/
        CancellationToken ct);

    Task NotifyDownloaded(int vpnServerId, string fileName, string externalId, /*todo: int actorUserId,*/
        bool isRevoked, CancellationToken ct);
}