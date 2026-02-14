using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.NotificationRecipientTable;

public interface INotificationRecipientQueryService
{
    /// <summary>
    /// Список уведомлений для админа с флагами IsRead/ReadAt (агрегация по БД).
    /// </summary>
    Task<List<NotificationListRow>> GetNotificationListByAdminUserIdAsync(int adminUserId, CancellationToken ct = default);
    Task<int> GetUnreadCountByAdminUserIdAsync(int adminUserId, CancellationToken ct = default);
}
