using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Requests;

namespace DataGateMonitor.Services.RfAvailability;

public interface IRfAvailabilityNotificationService
{
    Task NotifyUnreachableAsync(string targetUrl, string summary, CancellationToken ct);

    Task NotifyRecoveredAsync(string targetUrl, string summary, CancellationToken ct);
}

public sealed class RfAvailabilityNotificationService(INotificationService notifications)
    : IRfAvailabilityNotificationService
{
    private static readonly string[] Channels = ["web", "telegram"];
    private const string Source = "rf-availability";

    public Task NotifyUnreachableAsync(string targetUrl, string summary, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.RfAvailabilityUnreachable,
            Title = "Service unreachable from RF",
            Message = $"Target={targetUrl}. {summary}",
            Severity = NotificationSeverity.Warning,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.AppServerMonitorDown,
            DedupKey = $"rf-availability:unreachable:{targetUrl}",
        }, Channels, ct);

    public Task NotifyRecoveredAsync(string targetUrl, string summary, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.RfAvailabilityRecovered,
            Title = "Service reachable from RF again",
            Message = $"Target={targetUrl}. {summary}",
            Severity = NotificationSeverity.Info,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.AppServerMonitorUp,
            DedupKey = $"rf-availability:recovered:{targetUrl}",
        }, Channels, ct);
}
