using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.NotificationRecipientTable;

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

    public async Task<IPagedResult<NotificationListRow>> GetNotificationListPageByAdminUserIdAsync(int adminUserId, GetNotificationsRequest request, CancellationToken ct = default)
    {
        var page = request.Page;
        var pageSize = request.PageSize;
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var agg = recipientQuery
            .Query(asNoTracking: true)
            .Where(r => r.AdminUserId == adminUserId)
            .GroupBy(r => r.NotificationId)
            .Select(g => new { NotificationId = g.Key, IsRead = g.Any(r => r.ReadAt != null), ReadAt = g.Min(r => r.ReadAt) });

        var joined = from a in agg
                     join n in notificationQuery.Query(asNoTracking: true) on a.NotificationId equals n.Id
                     select new { a, n };

        var filtered = joined;
        if (request.IsRead.HasValue)
            filtered = filtered.Where(x => x.a.IsRead == request.IsRead.Value);

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var typeFilter = request.Type.Trim();
            filtered = filtered.Where(x => x.n.Type == typeFilter);
        }

        var severities = request.Severities;
        if (severities is { Length: > 0 })
            filtered = filtered.Where(x => severities.Contains(x.n.Severity));

        var ordered = filtered.OrderByDescending(x => x.n.CreateDate);
        var projected = ordered.Select(x => new NotificationListRow(x.n.Id, x.n.Type, (int)x.n.Severity, x.n.Title, x.n.Message, x.a.IsRead, x.n.CreateDate, x.a.ReadAt));

        var total = await projected.CountAsync(ct);
        var items = await projected.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResponse<NotificationListRow>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
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
