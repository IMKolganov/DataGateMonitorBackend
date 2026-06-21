using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications.OvpnFileApi;

public class OvpnFileNotificationService(INotificationService notifications) : IOvpnFileNotificationService
{
    private static readonly string[] ReadChannels = ["web"];
    private static readonly string[] ChangeChannels = ["web", "telegram"];

    public Task NotifyReadByToken(string token, int fileId, int vpnServerId, bool isRevoked, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.by-token" : "ovpn.read.by-token",
            $"{label} requested by token",
            $"Token={Short(token)}; FileId={fileId}; Revoked={isRevoked}", vpnServerId, NotificationSeverity.Info,
            ReadChannels, ct);
    }

    public Task NotifyReadAll(int vpnServerId, int count, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client links" : "OpenVPN profiles";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.all" : "ovpn.read.all",
            $"All {label} listed",
            $"ServerId={vpnServerId}; Count={count}", vpnServerId, NotificationSeverity.Info, ReadChannels, ct);
    }

    public Task NotifyReadAllWithToken(int vpnServerId, int count, bool isRevoked, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client links" : "OpenVPN profiles";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.all-with-token" : "ovpn.read.all-with-token",
            $"All {label} (with tokens) listed",
            $"ServerId={vpnServerId}; Count={count}; Revoked={isRevoked}", vpnServerId, NotificationSeverity.Info,
            ReadChannels, ct);
    }

    public Task NotifyReadByExternalId(string externalId, int count, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client links" : "OpenVPN profiles";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.by-external" : "ovpn.read.by-external",
            $"{label} listed by external id",
            $"ExternalId={externalId}; Count={count};", null, NotificationSeverity.Info, ReadChannels, ct);
    }

    public Task NotifyReadByExternalIdAndVpnServerId(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client links" : "OpenVPN profiles";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.by-external" : "ovpn.read.by-external",
            $"{label} listed by external id",
            $"ServerId={vpnServerId}; ExternalId={externalId}; Count={count}; Revoked={isRevoked}", vpnServerId,
            NotificationSeverity.Info, ReadChannels, ct);
    }

    public Task NotifyReadByExternalIdWithToken(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client links" : "OpenVPN profiles";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.by-external-with-token" : "ovpn.read.by-external-with-token",
            $"{label} (with tokens) listed by external id",
            $"ServerId={vpnServerId}; ExternalId={externalId}; Count={count}; Revoked={isRevoked}", vpnServerId,
            NotificationSeverity.Info, ReadChannels, ct);
    }

    public Task NotifyIssued(int vpnServerId, int fileId, string fileName, string externalId, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Mutate),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.issued" : "ovpn.issued",
            $"{label} issued",
            $"FileId={fileId}; FileName={fileName}; ExternalId={externalId}", vpnServerId, NotificationSeverity.Info,
            ChangeChannels, ct);
    }

    public Task NotifyIssuedWithToken(int vpnServerId, int fileId, string fileName, string externalId, int tokenId,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Mutate),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.issued" : "ovpn.issued",
            $"{label} (with token) issued",
            $"FileId={fileId}; FileName={fileName}; ExternalId={externalId}; TokenId={tokenId}", vpnServerId,
            NotificationSeverity.Info, ChangeChannels, ct);
    }

    public Task NotifyRevoked(int vpnServerId, int fileId, string fileName, string externalId, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Mutate),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.revoked" : "ovpn.revoked",
            $"{label} revoked",
            $"FileId={fileId}; FileName={fileName}; ExternalId={externalId}", vpnServerId, NotificationSeverity.Warning,
            ChangeChannels, ct);
    }

    public Task NotifyDownloaded(int vpnServerId, string fileName, string externalId, bool isRevoked, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        return NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Download),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.downloaded" : "ovpn.downloaded",
            $"{label} downloaded",
            $"FileName={fileName}; ExternalId={externalId}; Revoked={isRevoked}", vpnServerId,
            NotificationSeverity.Info, ChangeChannels, ct);
    }

    private Task NotifyAsync(
        VpnProfileNotificationStack stack,
        ApplicationNotificationKind preferenceKind,
        string type,
        string title,
        string message,
        int? serverId,
        NotificationSeverity severity,
        string[] channels,
        CancellationToken ct,
        int? actorUserId = null)
    {
        var source = stack == VpnProfileNotificationStack.Xray ? "xray-client-links" : "openvpn-files";
        return notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            Source = source,
            ServerId = serverId,
            ActorUserId = actorUserId,
            PreferenceKind = preferenceKind
        }, channels, ct);
    }

    private static string Short(string token)
        => string.IsNullOrEmpty(token) ? "" : (token.Length > 8 ? $"{token[..4]}…{token[^4..]}" : token);
}
