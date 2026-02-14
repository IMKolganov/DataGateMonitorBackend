using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.NotificationRecipientTable;

public interface INotificationRecipientQueryService
{
    /// <summary>
    /// List of notifications for an admin with IsRead/ReadAt flags (aggregation done in DB).
    /// </summary>
    Task<List<NotificationListRow>> GetNotificationListByAdminUserIdAsync(int adminUserId, CancellationToken ct = default);
    Task<int> GetUnreadCountByAdminUserIdAsync(int adminUserId, CancellationToken ct = default);
}
