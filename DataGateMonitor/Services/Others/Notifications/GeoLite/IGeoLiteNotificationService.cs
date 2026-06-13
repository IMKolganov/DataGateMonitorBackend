namespace DataGateMonitor.Services.Others.Notifications.GeoLite;

public interface IGeoLiteNotificationService
{
    Task NotifyAutoUpdateSucceededAsync(string databasePath, CancellationToken ct);

    Task NotifyAutoUpdateFailedAsync(string phase, string detail, CancellationToken ct);
}
