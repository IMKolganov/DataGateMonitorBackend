using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

namespace DataGateMonitor.Services.Others.Notifications.TrafficDaily;

public interface ITrafficDailyRollupNotificationService
{
    Task NotifyCatchUpSucceededAsync(TrafficDailyRollupCatchUpResult result, CancellationToken ct);

    Task NotifyCatchUpFailedAsync(TrafficDailyRollupDayFailure failure, CancellationToken ct);

    Task NotifyCatchUpFailedAsync(string phase, string detail, CancellationToken ct);
}
