using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.SharedModels.Notifications.Responses;

namespace OpenVPNGateMonitor.Services.Others;

/// <summary>
/// Handles creation and delivery of system notifications to admin users.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a notification and delivers it to all admins via the specified channels.
    /// If <paramref name="channels"/> is null or empty, all available channels are used.
    /// Returns the created notification ID.
    /// </summary>
    Task<int> NotifyAdmins(
        NotificationRequest request,
        IEnumerable<string>? channels = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a notification as successfully delivered ("sent") for a specific admin and channel.
    /// </summary>
    Task MarkDelivered(
        int notificationId,
        int adminUserId,
        string channel,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a notification as read by the specified admin (for all channels).
    /// </summary>
    Task MarkRead(
        int notificationId,
        int adminUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all notifications for the given admin user (from NotificationRecipient).
    /// </summary>
    Task<GetAllNotificationsResponse> GetAllForUserAsync(int adminUserId, CancellationToken ct = default);

    /// <summary>
    /// Returns the count of notifications not yet read by the given admin user.
    /// </summary>
    Task<int> GetUnreadCountAsync(int adminUserId, CancellationToken ct = default);
}