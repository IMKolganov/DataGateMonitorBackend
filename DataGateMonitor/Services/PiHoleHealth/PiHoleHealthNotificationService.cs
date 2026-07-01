using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Requests;

namespace DataGateMonitor.Services.PiHoleHealth;

public sealed class PiHoleHealthNotificationService(INotificationService notifications) : IPiHoleHealthNotificationService
{
    private static readonly string[] Channels = ["web", "telegram"];
    private const string Source = "pihole-health";

    public Task NotifyUnhealthyAsync(
        int vpnServerId,
        string serverName,
        string health,
        string healthMessage,
        CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.PiHoleCollectorUnhealthy,
            Title = $"Pi-hole integration unhealthy ({health})",
            Message = $"Server={serverName} (id {vpnServerId}). {healthMessage}",
            Severity = string.Equals(health, "Error", StringComparison.OrdinalIgnoreCase)
                ? NotificationSeverity.Error
                : NotificationSeverity.Warning,
            Source = Source,
            ServerId = vpnServerId,
            PreferenceKind = ApplicationNotificationKind.OpenVpnServerSyncError
        }, Channels, ct);

    public Task NotifyRecoveredAsync(int vpnServerId, string serverName, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.PiHoleCollectorRecovered,
            Title = "Pi-hole integration recovered",
            Message = $"Server={serverName} (id {vpnServerId}). Collector and diagnostics are healthy again.",
            Severity = NotificationSeverity.Info,
            Source = Source,
            ServerId = vpnServerId,
            PreferenceKind = ApplicationNotificationKind.OpenVpnServerSyncError
        }, Channels, ct);
}
