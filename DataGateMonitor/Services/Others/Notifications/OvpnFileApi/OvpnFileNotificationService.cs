using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.Others.Notifications.OvpnFileApi;

public class OvpnFileNotificationService(
    INotificationService notifications,
    ApplicationDbContext db) : IOvpnFileNotificationService
{
    private static readonly string[] ReadChannels = ["web", "telegram"];
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

    public async Task NotifyReadByExternalId(string externalId, int count, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client links" : "OpenVPN profiles";
        var displayName = await ResolveDisplayNameAsync(externalId, ct);
        await NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.by-external" : "ovpn.read.by-external",
            $"{label} listed by external id",
            AppendDisplayName($"ExternalId={externalId}; Count={count};", displayName), null,
            NotificationSeverity.Info, ReadChannels, ct);
    }

    public async Task NotifyReadByExternalIdAndVpnServerId(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client links" : "OpenVPN profiles";
        var displayName = await ResolveDisplayNameAsync(externalId, ct);
        await NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.by-external" : "ovpn.read.by-external",
            $"{label} listed by external id",
            AppendDisplayName(
                $"ServerId={vpnServerId}; ExternalId={externalId}; Count={count}; Revoked={isRevoked}",
                displayName),
            vpnServerId, NotificationSeverity.Info, ReadChannels, ct);
    }

    public async Task NotifyReadByExternalIdWithToken(int vpnServerId, string externalId, int count, bool isRevoked,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client links" : "OpenVPN profiles";
        var displayName = await ResolveDisplayNameAsync(externalId, ct);
        await NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Read),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.read.by-external-with-token" : "ovpn.read.by-external-with-token",
            $"{label} (with tokens) listed by external id",
            AppendDisplayName(
                $"ServerId={vpnServerId}; ExternalId={externalId}; Count={count}; Revoked={isRevoked}",
                displayName),
            vpnServerId, NotificationSeverity.Info, ReadChannels, ct);
    }

    public async Task NotifyIssued(int vpnServerId, int fileId, string fileName, string externalId, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        var displayName = await ResolveDisplayNameAsync(externalId, ct);
        await NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Mutate),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.issued" : "ovpn.issued",
            $"{label} issued",
            AppendDisplayName($"FileId={fileId}; FileName={fileName}; ExternalId={externalId}", displayName),
            vpnServerId, NotificationSeverity.Info, ChangeChannels, ct);
    }

    public async Task NotifyIssuedWithToken(int vpnServerId, int fileId, string fileName, string externalId, int tokenId,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        var displayName = await ResolveDisplayNameAsync(externalId, ct);
        await NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Mutate),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.issued" : "ovpn.issued",
            $"{label} (with token) issued",
            AppendDisplayName(
                $"FileId={fileId}; FileName={fileName}; ExternalId={externalId}; TokenId={tokenId}",
                displayName),
            vpnServerId, NotificationSeverity.Info, ChangeChannels, ct);
    }

    public async Task NotifyRevoked(int vpnServerId, int fileId, string fileName, string externalId, CancellationToken ct,
        VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        var displayName = await ResolveDisplayNameAsync(externalId, ct);
        await NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Mutate),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.revoked" : "ovpn.revoked",
            $"{label} revoked",
            AppendDisplayName($"FileId={fileId}; FileName={fileName}; ExternalId={externalId}", displayName),
            vpnServerId, NotificationSeverity.Warning, ChangeChannels, ct);
    }

    public async Task NotifyDownloaded(int vpnServerId, string fileName, string externalId, bool isRevoked,
        CancellationToken ct, VpnProfileNotificationStack stack = VpnProfileNotificationStack.OpenVpn)
    {
        var label = stack == VpnProfileNotificationStack.Xray ? "Xray client link" : "OpenVPN profile";
        var displayName = await ResolveDisplayNameAsync(externalId, ct);
        await NotifyAsync(
            stack,
            VpnProfileNotificationKindMapping.FromStackAndCategory(stack, VpnProfileNotificationCategory.Download),
            stack == VpnProfileNotificationStack.Xray ? "xray.vless.downloaded" : "ovpn.downloaded",
            $"{label} downloaded",
            AppendDisplayName($"FileName={fileName}; ExternalId={externalId}; Revoked={isRevoked}", displayName),
            vpnServerId, NotificationSeverity.Info, ChangeChannels, ct);
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

    private async Task<string?> ResolveDisplayNameAsync(string? externalId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(externalId)) return null;
        var userId = await db.UserIdentityLinks.AsNoTracking()
            .Where(l => l.ExternalId == externalId)
            .OrderBy(l => l.Id)
            .Select(l => (int?)l.UserId)
            .FirstOrDefaultAsync(ct);
        if (userId is null) return null;
        var name = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    private static string AppendDisplayName(string message, string? displayName)
        => string.IsNullOrWhiteSpace(displayName) ? message : $"{message}; DisplayName={displayName}";

    private static string Short(string token)
        => string.IsNullOrEmpty(token) ? "" : (token.Length > 8 ? $"{token[..4]}…{token[^4..]}" : token);
}
