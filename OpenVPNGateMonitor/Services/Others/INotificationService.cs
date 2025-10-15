using OpenVPNGateMonitor.Services.Others.Models;

namespace OpenVPNGateMonitor.Services.Others;

using OpenVPNGateMonitor.SharedModels.Enums;

public interface INotificationService
{
    /// <summary>
    /// Notify all admins via given channels.
    /// </summary>
    Task<int> NotifyAdminsAsync(
        NotificationRequest request,
        IEnumerable<string>? channels = null,
        CancellationToken ct = default);

    /// <summary>
    /// Mark a notification as delivered for a recipient+channel.
    /// </summary>
    Task MarkDeliveredAsync(int notificationId, int adminUserId, string channel, CancellationToken ct = default);

    /// <summary>
    /// Mark a notification as read by a recipient.
    /// </summary>
    Task MarkReadAsync(int notificationId, int adminUserId, CancellationToken ct = default);
}
