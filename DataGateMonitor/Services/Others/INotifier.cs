namespace DataGateMonitor.Services.Others;

using DataGateMonitor.Models;

public interface INotifier
{
    /// <summary>
    /// Unique delivery channel identifier (e.g. "web", "telegram", "email").
    /// </summary>
    string Channel { get; }

    /// <summary>
    /// Sends a notification to a specific admin.
    /// </summary>
    /// <param name="notification">Notification entity to send.</param>
    /// <param name="adminUserId">Recipient admin user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task Send(Notification notification, int adminUserId, CancellationToken ct);
}
