using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications.OvpnFileApi;

public interface IOvpnFileNotificationService
{
    Task NotifyReadByToken(string token, int fileId, int vpnServerId, bool isRevoked, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyReadAll(int vpnServerId, int count, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyReadAllWithToken(int vpnServerId, int count, bool isRevoked, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyReadByExternalId(string externalId, int count, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyReadByExternalIdAndVpnServerId(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyReadByExternalIdWithToken(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyIssued(int vpnServerId, int fileId, string fileName, string externalId, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyIssuedWithToken(int vpnServerId, int fileId, string fileName, string externalId, int tokenId,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyRevoked(int vpnServerId, int fileId, string fileName, string externalId, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);

    Task NotifyDownloaded(int vpnServerId, string fileName, string externalId, bool isRevoked, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn);
}
