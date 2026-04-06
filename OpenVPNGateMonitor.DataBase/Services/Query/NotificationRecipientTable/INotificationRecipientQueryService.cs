using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Notifications.Requests;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.NotificationRecipientTable;

public interface INotificationRecipientQueryService
{
    /// <summary>
    /// List of notifications for an admin with IsRead/ReadAt flags (aggregation done in DB).
    /// </summary>
    Task<List<NotificationListRow>> GetNotificationListByAdminUserIdAsync(int adminUserId, CancellationToken ct = default);

    /// <summary>
    /// Paged list of notifications for an admin (same as above with Skip/Take). Uses filter fields from <paramref name="request"/> (read state, severities, type).
    /// </summary>
    Task<IPagedResult<NotificationListRow>> GetNotificationListPageByAdminUserIdAsync(int adminUserId, GetNotificationsRequest request, CancellationToken ct = default);

    Task<int> GetUnreadCountByAdminUserIdAsync(int adminUserId, CancellationToken ct = default);
}
