using DataGateMonitor.Services.Others.Models;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications.ServerOpenVpnApiClient;

public class ServerOpenVpnNotificationService(INotificationService notifications) : IServerOpenVpnNotificationService
{
    private static readonly string[] InfoChannels = ["web", "telegram"];
    private static readonly string[] ErrorChannels = ["web", "telegram"];
    private const string Source = "server-openvpn-api";

    public Task NotifyBecameAvailable(int serverId, string? serverName, CancellationToken ct)
        => Notify(ApplicationNotificationKind.OpenVpnServerBecameAvailable, "server.became-available", "OpenVPN server became available",
            Detail(serverId, serverName), serverId, NotificationSeverity.Info, InfoChannels, ct);

    public Task NotifyAdded(int serverId, string? serverName, CancellationToken ct)
        => Notify(ApplicationNotificationKind.OpenVpnServerAdded, "server.added", "OpenVPN server added",
            Detail(serverId, serverName), serverId, NotificationSeverity.Info, InfoChannels, ct);

    public Task NotifyUpdated(int serverId, string? serverName, CancellationToken ct)
        => Notify(ApplicationNotificationKind.OpenVpnServerUpdated, "server.updated", "OpenVPN server updated",
            Detail(serverId, serverName), serverId, NotificationSeverity.Info, InfoChannels, ct);

    public Task NotifyDeleted(int serverId, string? serverName, CancellationToken ct)
        => Notify(ApplicationNotificationKind.OpenVpnServerDeleted, "server.deleted", "OpenVPN server deleted",
            Detail(serverId, serverName), serverId, NotificationSeverity.Warning, InfoChannels, ct);

    public Task NotifyBecameUnavailableDueToError(int serverId, string? serverName, string? errorMessage, CancellationToken ct)
        => Notify(ApplicationNotificationKind.OpenVpnServerBecameUnavailable, "server.became-unavailable", "OpenVPN server became unavailable due to error",
            Detail(serverId, serverName) + (string.IsNullOrEmpty(errorMessage) ? "" : $"; Error={errorMessage}"),
            serverId, NotificationSeverity.Error, ErrorChannels, ct);

    public Task NotifySyncError(int serverId, string? serverName, string? errorMessage, CancellationToken ct)
        => Notify(ApplicationNotificationKind.OpenVpnServerSyncError, "server.sync-error", "Synchronization error with OpenVPN server",
            Detail(serverId, serverName) + (string.IsNullOrEmpty(errorMessage) ? "" : $"; Error={errorMessage}"),
            serverId, NotificationSeverity.Error, ErrorChannels, ct);

    public Task NotifyNoResponseFromServer(int serverId, string? serverName, CancellationToken ct)
        => Notify(ApplicationNotificationKind.OpenVpnServerNoResponse, "server.no-response", "No response from OpenVPN server",
            Detail(serverId, serverName), serverId, NotificationSeverity.Warning, ErrorChannels, ct);

    private static string Detail(int serverId, string? serverName)
        => $"ServerId={serverId}" + (string.IsNullOrEmpty(serverName) ? "" : $"; Name={serverName}");

    private Task Notify(ApplicationNotificationKind preferenceKind, string type, string title, string message, int serverId, NotificationSeverity severity,
        string[] channels, CancellationToken ct)
        => notifications.NotifyAdmins(new NotificationRequest
        {
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            Source = Source,
            ServerId = serverId,
            PreferenceKind = preferenceKind
        }, channels, ct);
}
