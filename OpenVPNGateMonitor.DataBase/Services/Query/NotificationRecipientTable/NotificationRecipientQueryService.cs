using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.NotificationRecipientTable;

public class NotificationRecipientQueryService(
    IQueryService<NotificationRecipient, int> recipientQuery,
    IQueryService<Notification, int> notificationQuery) : INotificationRecipientQueryService
{
    public async Task<List<NotificationListRow>> GetNotificationListByAdminUserIdAsync(int adminUserId, CancellationToken ct = default)
    {
        var agg = recipientQuery
            .Query(asNoTracking: true)
            .Where(r => r.AdminUserId == adminUserId)
            .GroupBy(r => r.NotificationId)
            .Select(g => new { NotificationId = g.Key, IsRead = g.Any(r => r.ReadAt != null), ReadAt = g.Min(r => r.ReadAt) });

        var list = await (
            from a in agg
            join n in notificationQuery.Query(asNoTracking: true) on a.NotificationId equals n.Id
            orderby n.CreateDate descending
            select new NotificationListRow(n.Id, n.Type, (int)n.Severity, n.Title, n.Message, a.IsRead, n.CreateDate, a.ReadAt)
        ).ToListAsync(ct);

        return list;
    }

    public async Task<int> GetUnreadCountByAdminUserIdAsync(int adminUserId, CancellationToken ct = default)
    {
        return await recipientQuery
            .Query(asNoTracking: true)
            .Where(r => r.AdminUserId == adminUserId)
            .GroupBy(r => r.NotificationId)
            .CountAsync(g => g.All(r => r.ReadAt == null), ct);
    }
}
