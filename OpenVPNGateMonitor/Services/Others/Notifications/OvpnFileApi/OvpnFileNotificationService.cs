using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Services.Others.Notifications.OvpnFileApi;

public class OvpnFileNotificationService(INotificationService notifications) : IOvpnFileNotificationService
{
    private static readonly string[] ReadChannels = ["web"];
    private static readonly string[] ChangeChannels = ["web", "telegram"];

    public Task NotifyReadByToken(string token, int fileId, int vpnServerId, bool isRevoked, CancellationToken ct)
        => Notify("ovpn.read.by-token", "OVPN file requested by token",
            $"Token={Short(token)}; FileId={fileId}; Revoked={isRevoked}", vpnServerId, NotificationSeverity.Info,
            ReadChannels, ct);

    public Task NotifyReadAll(int vpnServerId, int count, CancellationToken ct)
        => Notify("ovpn.read.all", "All OVPN files requested",
            $"ServerId={vpnServerId}; Count={count}", vpnServerId, NotificationSeverity.Info, ReadChannels, ct);

    public Task NotifyReadAllWithToken(int vpnServerId, int count, bool isRevoked, CancellationToken ct)
        => Notify("ovpn.read.all-with-token", "All OVPN files with tokens requested",
            $"ServerId={vpnServerId}; Count={count}; Revoked={isRevoked}", vpnServerId, NotificationSeverity.Info,
            ReadChannels, ct);

    public Task NotifyReadByExternalId(string externalId, int count,
        CancellationToken ct)
        => Notify("ovpn.read.by-external", "OVPN files requested by ExternalId",
            $"ExternalId={externalId}; Count={count};", null,
            NotificationSeverity.Info, ReadChannels, ct);
    public Task NotifyReadByExternalIdAndVpnServerId(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct)
        => Notify("ovpn.read.by-external", "OVPN files requested by ExternalId",
            $"ServerId={vpnServerId}; ExternalId={externalId}; Count={count}; Revoked={isRevoked}", vpnServerId,
            NotificationSeverity.Info, ReadChannels, ct);

    public Task NotifyReadByExternalIdWithToken(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct)
        => Notify("ovpn.read.by-external-with-token", "OVPN files (with tokens) requested by ExternalId",
            $"ServerId={vpnServerId}; ExternalId={externalId}; Count={count}; Revoked={isRevoked}", vpnServerId,
            NotificationSeverity.Info, ReadChannels, ct);

    public Task NotifyIssued(int vpnServerId, int fileId, string fileName, string externalId, 
        CancellationToken ct)
        => Notify("ovpn.issued", "OVPN file issued",
            $"FileId={fileId}; FileName={fileName}; ExternalId={externalId}", vpnServerId, NotificationSeverity.Info,
            ChangeChannels, ct);

    public Task NotifyIssuedWithToken(int vpnServerId, int fileId, string fileName, string externalId, int tokenId, 
        CancellationToken ct)
        => Notify("ovpn.issued", "OVPN file (with token) issued",
            $"FileId={fileId}; FileName={fileName}; ExternalId={externalId}; TokenId={tokenId}", vpnServerId,
            NotificationSeverity.Info, ChangeChannels, ct);

    public Task NotifyRevoked(int vpnServerId, int fileId, string fileName, string externalId,
        CancellationToken ct)
        => Notify("ovpn.revoked", "OVPN file revoked",
            $"FileId={fileId}; FileName={fileName}; ExternalId={externalId}", vpnServerId, NotificationSeverity.Warning,
            ChangeChannels, ct);

    public Task NotifyDownloaded(int vpnServerId, string fileName, string externalId,
        bool isRevoked, CancellationToken ct)
        => Notify("ovpn.downloaded", "OVPN file downloaded",
            $"FileName={fileName}; ExternalId={externalId}; Revoked={isRevoked}", vpnServerId,
            NotificationSeverity.Info, ChangeChannels, ct);

    // ---- helper ----
    private Task Notify(string type, string title, string message, int? serverId, NotificationSeverity severity,
        string[] channels, CancellationToken ct, int? actorUserId = null)
        => notifications.NotifyAdmins(new NotificationRequest
        {
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            Source = "ovpn-api",
            ServerId = serverId,
            ActorUserId = actorUserId
        }, channels, ct);

    private static string Short(string token)
        => string.IsNullOrEmpty(token) ? "" : (token.Length > 8 ? $"{token[..4]}…{token[^4..]}" : token);
}
