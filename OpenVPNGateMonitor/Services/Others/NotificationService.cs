using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.NotificationRecipientTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserRoleTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.SharedModels.Auth;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Notifications.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Services.Others;

public class NotificationService(
    ICommandService<Notification, int> notificationCommandServices,
    IUserRoleQueryService userRoleQueryService,
    ICommandService<NotificationRecipient, int> notificationRecipientCommandServices,
    INotificationRecipientQueryService notificationRecipientQueryService,
    Dictionary<string, INotifier> notifiersByChannel,
    ILogger<NotificationService> logger
) : INotificationService
{
    public async Task<int> NotifyAdmins(
        NotificationRequest request,
        IEnumerable<string>? channels = null,
        CancellationToken ct = default)
    {
        // 1) Resolve recipients: users with Admin role (User.Id), same identity as dashboard and JWT
        var adminUserIds = await userRoleQueryService.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, ct);
        if (adminUserIds.Count == 0)
        {
            logger.LogError("No admins found (role Admin). Skipping notification: {Type} - {Title}", request.Type, request.Title);
            return 0;
        }

        // 2) Resolve channels
        var selectedChannels = (channels?.Where(c => !string.IsNullOrWhiteSpace(c)).ToList())
                               ?? notifiersByChannel.Keys.ToList();

        var activeNotifiers = selectedChannels
            .Select(c => notifiersByChannel.TryGetValue(c, out var n) ? n : null)
            .Where(n => n is not null)
            .Cast<INotifier>()
            .ToList();

        if (activeNotifiers.Count == 0)
        {
            logger.LogWarning("No active notifiers for channels: {Channels}. Notification will be persisted only.", string.Join(",", selectedChannels));
        }

        // 3) Persist notification
        var now = DateTimeOffset.UtcNow;
        var notification = new Notification
        {
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            Severity = request.Severity,
            Source = request.Source,
            ServerId = request.ServerId,
            ActorUserId = request.ActorUserId,
            RelatedClientId = request.RelatedClientId,
            CorrelationId = request.CorrelationId,
            DedupKey = request.DedupKey,
            CreateDate = now,
            LastUpdate = now
        };

        var created = await notificationCommandServices.Add(notification, saveChanges: true, ct);
        var notificationId = created.Id;

        // 4) Persist recipients (admin user id × channel)
        var recipients = (from adminUserId in adminUserIds
                          from notifier in activeNotifiers
                          select new NotificationRecipient
                          {
                              NotificationId = notificationId,
                              AdminUserId = adminUserId,
                              DeliveryChannel = notifier.Channel,
                              DeliveryStatus = DeliveryStatus.Pending,
                              CreateDate = now,
                              LastUpdate = now
                          }).ToList();

        if (recipients.Count > 0)
            await notificationRecipientCommandServices.AddRange(recipients, saveChanges: true, ct);

        // 5) Fan-out sending (best-effort). Sequential to avoid NpgsqlOperationInProgressException (single DbContext/connection).
        if (activeNotifiers.Count > 0)
        {
            try
            {
                foreach (var adminUserId in adminUserIds)
                {
                    foreach (var notifier in activeNotifiers)
                    {
                        await SendSafe(notifier, created, adminUserId, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during fanout for NotificationId={NotificationId}", notificationId);
            }
        }

        return notificationId;
    }

    public Task MarkDelivered(int notificationId, int adminUserId, string channel, CancellationToken ct = default)
    {
        // Update status to Sent (successfully delivered)
        return notificationRecipientCommandServices.UpdateWhere(
            predicate: r => r.NotificationId == notificationId
                            && r.AdminUserId == adminUserId
                            && r.DeliveryChannel == channel,
            set: calls => calls
                .SetProperty(r => r.DeliveryStatus, DeliveryStatus.Sent)
                .SetProperty(r => r.DeliveredAt, DateTimeOffset.UtcNow)
                .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
            ct: ct
        );
    }

    public Task MarkRead(int notificationId, int adminUserId, CancellationToken ct = default)
    {
        // Mark as read (applies to all recipient channels for this admin)
        return notificationRecipientCommandServices.UpdateWhere(
            predicate: r => r.NotificationId == notificationId
                            && r.AdminUserId == adminUserId,
            set: calls => calls
                .SetProperty(r => r.DeliveryStatus, DeliveryStatus.Read)
                .SetProperty(r => r.ReadAt, DateTimeOffset.UtcNow)
                .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
            ct: ct
        );
    }

    public async Task<GetAllNotificationsResponse> GetAllForUserAsync(int adminUserId, CancellationToken ct = default)
    {
        var rows = await notificationRecipientQueryService.GetNotificationListByAdminUserIdAsync(adminUserId, ct);
        var items = rows.Select(r => new NotificationItemDto
        {
            Id = r.Id,
            Type = r.Type,
            Severity = (NotificationSeverity)r.Severity,
            Title = r.Title,
            Message = r.Message,
            IsRead = r.IsRead,
            CreatedAt = r.CreatedAt,
            ReadAt = r.ReadAt
        }).ToList();
        return new GetAllNotificationsResponse { Notifications = items };
    }

    public async Task<GetNotificationsResponse> GetPageForUserAsync(int adminUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var paged = await notificationRecipientQueryService.GetNotificationListPageByAdminUserIdAsync(adminUserId, page, pageSize, ct);
        var items = paged.Items.Select(r => new NotificationItemDto
        {
            Id = r.Id,
            Type = r.Type,
            Severity = (NotificationSeverity)r.Severity,
            Title = r.Title,
            Message = r.Message,
            IsRead = r.IsRead,
            CreatedAt = r.CreatedAt,
            ReadAt = r.ReadAt
        }).ToList();
        return new GetNotificationsResponse
        {
            Notifications = new PagedResponse<NotificationItemDto>
            {
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                Items = items
            }
        };
    }

    public Task<int> GetUnreadCountAsync(int adminUserId, CancellationToken ct = default)
        => notificationRecipientQueryService.GetUnreadCountByAdminUserIdAsync(adminUserId, ct);

    public Task MarkReadAllAsync(int adminUserId, CancellationToken ct = default)
    {
        return notificationRecipientCommandServices.UpdateWhere(
            predicate: r => r.AdminUserId == adminUserId && r.ReadAt == null,
            set: calls => calls
                .SetProperty(r => r.DeliveryStatus, DeliveryStatus.Read)
                .SetProperty(r => r.ReadAt, DateTimeOffset.UtcNow)
                .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
            ct: ct
        );
    }

    // ----------------------
    // Helpers
    // ----------------------
    private async Task SendSafe(INotifier notifier, Notification notification, int adminUserId, CancellationToken ct)
    {
        try
        {
            await notifier.Send(notification, adminUserId, ct);

            // mark as Sent on success
            await notificationRecipientCommandServices.UpdateWhere(
                predicate: r => r.NotificationId == notification.Id
                                && r.AdminUserId == adminUserId
                                && r.DeliveryChannel == notifier.Channel,
                set: calls => calls
                    .SetProperty(r => r.DeliveryStatus, DeliveryStatus.Sent)
                    .SetProperty(r => r.DeliveredAt, DateTimeOffset.UtcNow)
                    .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
                ct: ct
            );
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation(
                "Send canceled: NotificationId={NotificationId}, AdminId={AdminId}, Channel={Channel}",
                notification.Id, adminUserId, notifier.Channel);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send NotificationId={NotificationId} via {Channel} to AdminId={AdminId}",
                notification.Id, notifier.Channel, adminUserId);

            // mark as Failed
            await notificationRecipientCommandServices.UpdateWhere(
                predicate: r => r.NotificationId == notification.Id
                                && r.AdminUserId == adminUserId
                                && r.DeliveryChannel == notifier.Channel,
                set: calls => calls
                    .SetProperty(r => r.DeliveryStatus, DeliveryStatus.Failed)
                    .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
                ct: ct
            );
        }
    }
}
