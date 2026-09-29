using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Requests;

namespace DataGateMonitor.Services.AvailabilityCheck;

public interface IAvailabilityCheckNotificationService
{
    Task NotifyUnreachableAsync(string targetUrl, string summary, CancellationToken ct);

    Task NotifyRecoveredAsync(string targetUrl, string summary, CancellationToken ct);
}

public sealed class AvailabilityCheckNotificationService(INotificationService notifications)
    : IAvailabilityCheckNotificationService
{
    private static readonly string[] Channels = ["web", "telegram"];
    private const string Source = "availability-check";

    public Task NotifyUnreachableAsync(string targetUrl, string summary, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.AvailabilityCheckUnreachable,
            Title = "Service unreachable (availability probe)",
            Message = $"Target={targetUrl}. {summary}",
            Severity = NotificationSeverity.Warning,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.AppServerMonitorDown,
            DedupKey = $"availability-check:unreachable:{targetUrl}",
        }, Channels, ct);

    public Task NotifyRecoveredAsync(string targetUrl, string summary, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.AvailabilityCheckRecovered,
            Title = "Service reachable again (availability probe)",
            Message = $"Target={targetUrl}. {summary}",
            Severity = NotificationSeverity.Info,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.AppServerMonitorUp,
            DedupKey = $"availability-check:recovered:{targetUrl}",
        }, Channels, ct);
}
