using DataGateMonitor.Models.Helpers;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications.GeoLite;

public sealed class GeoLiteNotificationService(INotificationService notifications) : IGeoLiteNotificationService
{
    private static readonly string[] Channels = ["web", "telegram"];
    private const string Source = "geolite-auto-update";

    public Task NotifyAutoUpdateSucceededAsync(string databasePath, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.GeoLiteAutoUpdateSucceeded,
            Title = "GeoLite database updated",
            Message = $"Automatic GeoLite2 update completed. Database path: {databasePath}",
            Severity = NotificationSeverity.Info,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.GeoLiteAutoUpdateSucceeded
        }, Channels, ct);

    public Task NotifyAutoUpdateFailedAsync(string phase, string detail, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.GeoLiteAutoUpdateFailed,
            Title = "GeoLite automatic update failed",
            Message = $"Phase: {phase}. {detail}",
            Severity = NotificationSeverity.Error,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.GeoLiteAutoUpdateFailed
        }, Channels, ct);

    public Task NotifyDiskFullAsync(string phase, string detail, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.GeoLiteAutoUpdateFailed,
            Title = "GeoLite update failed: disk full",
            Message =
                $"Phase: {phase}. No space left on device while updating GeoLite2. " +
                $"Prune old GeoLite2_* folders under the GeoIP DB path (setting GeoIp_Max_Kept_Versions) or free disk on the monitor host. {detail}",
            Severity = NotificationSeverity.Error,
            Source = Source,
            DedupKey = "geolite.auto-update.failed:disk-full",
            PreferenceKind = ApplicationNotificationKind.GeoLiteAutoUpdateFailed
        }, Channels, ct);
}
