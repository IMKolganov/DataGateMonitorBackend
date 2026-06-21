using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications.TrafficDaily;

public sealed class TrafficDailyRollupNotificationService(INotificationService notifications)
    : ITrafficDailyRollupNotificationService
{
    private static readonly string[] Channels = ["web", "telegram"];
    private const string Source = "traffic-daily-rollup";

    public Task NotifyCatchUpSucceededAsync(TrafficDailyRollupCatchUpResult result, CancellationToken ct)
    {
        var dayCount = result.ProcessedDays.Count;
        var range = dayCount == 1
            ? result.FirstDay!.Value.ToString("yyyy-MM-dd")
            : $"{result.FirstDay:yyyy-MM-dd} .. {result.LastDay:yyyy-MM-dd}";

        return notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.TrafficDailyRollupSucceeded,
            Title = "Traffic daily rollup completed",
            Message =
                $"Built daily traffic slices for {dayCount} UTC day(s) ({range}). " +
                $"Upserted {result.SessionDayRowsUpserted} session-day row(s).",
            Severity = NotificationSeverity.Info,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.TrafficDailyRollupSucceeded
        }, Channels, ct);
    }

    public Task NotifyCatchUpFailedAsync(TrafficDailyRollupDayFailure failure, CancellationToken ct)
    {
        var completed = failure.CompletedDaysBeforeFailure.Count;
        var completedPart = completed > 0
            ? $" Completed {completed} day(s) before failure."
            : string.Empty;

        return NotifyCatchUpFailedAsync(
            $"rollup day {failure.DayUtc:yyyy-MM-dd}",
            $"{failure.Exception.GetType().Name}: {failure.Exception.Message}.{completedPart}",
            ct);
    }

    public Task NotifyCatchUpFailedAsync(string phase, string detail, CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = NotificationTypes.TrafficDailyRollupFailed,
            Title = "Traffic daily rollup failed",
            Message = $"Phase: {phase}. {detail}",
            Severity = NotificationSeverity.Error,
            Source = Source,
            PreferenceKind = ApplicationNotificationKind.TrafficDailyRollupFailed
        }, Channels, ct);
}
