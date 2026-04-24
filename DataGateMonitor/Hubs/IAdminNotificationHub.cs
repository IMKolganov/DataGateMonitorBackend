using DataGateMonitor.Models;

namespace DataGateMonitor.Hubs;

/// <summary>
/// Contract for sending notifications to admin clients via SignalR.
/// </summary>
public interface IAdminNotificationHub
{
    /// <summary>
    /// Sends notification to a specific admin.
    /// </summary>
    Task SendNotificationAsync(int adminUserId, Notification notification, CancellationToken ct);
}