using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others.Models;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications.GeoLite;

public sealed class GeoLiteNotificationService(INotificationService notifications) : IGeoLiteNotificationService
{
    private static readonly string[] Channels = ["web", "telegram"];
    private const string Source = "geolite-auto-update";

    public Task NotifyAutoUpdateSucceededAsync(string databasePath, CancellationToken ct)
        => notifications.NotifyAdmins(new NotificationRequest
        {
            Type = NotificationTypes.GeoLiteAutoUpdateSucceeded,
            Title = "GeoLite database updated",
            Message = $"Automatic GeoLite2 update completed. Database path: {databasePath}",
            Severity = NotificationSeverity.Info,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.GeoLiteAutoUpdateSucceeded
        }, Channels, ct);

    public Task NotifyAutoUpdateFailedAsync(string phase, string detail, CancellationToken ct)
        => notifications.NotifyAdmins(new NotificationRequest
        {
            Type = NotificationTypes.GeoLiteAutoUpdateFailed,
            Title = "GeoLite automatic update failed",
            Message = $"Phase: {phase}. {detail}",
            Severity = NotificationSeverity.Error,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.GeoLiteAutoUpdateFailed
        }, Channels, ct);
}
