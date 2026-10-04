using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.NotificationRecipientTable;
using DataGateMonitor.DataBase.Services.Query.UserRoleTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Services.Others;

public class NotificationService(
    ICommandService<Notification, int> notificationCommandServices,
    IUserRoleQueryService userRoleQueryService,
    ICommandService<NotificationRecipient, int> notificationRecipientCommandServices,
    INotificationRecipientQueryService notificationRecipientQueryService,
    Dictionary<string, INotifier> notifiersByChannel,
    IVpnProfileNotificationPreferenceService vpnProfileNotificationPreferences,
    ILogger<NotificationService> logger
) : INotificationService
{
    private const int MaxDeliveryErrorLength = 1024;

    public async Task<int> NotifyAdmins(
        NotifyAdminsRequest request,
        IEnumerable<string>? channels = null,
        CancellationToken ct = default)
    {
        if (request.PreferenceKind is { } kind)
        {
            if (!await vpnProfileNotificationPreferences.IsApplicationNotificationAllowedAsync(kind, ct))
            {
                logger.LogDebug("Skipping admin notification (preference disabled): {Kind} {Type}", kind, request.Type);
                return 0;
            }
        }

        // 1) Resolve recipients: users with Admin role (User.Id), same identity as dashboard and JWT
        var adminUserIds = await userRoleQueryService.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, ct);
        if (adminUserIds.Count == 0)
        {
            logger.LogError("No admins found (role Admin). Skipping notification: {Type} - {Title}", request.Type, request.Title);
            return 0;
        }

        // 2) Resolve channels
        var selectedChannels = (channels?.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList())
                               ?? notifiersByChannel.Keys.ToList();

        var activeNotifiers = selectedChannels
            .Select(c => notifiersByChannel.TryGetValue(c, out var n) ? n : null)
            .Where(n => n is not null)
            .Cast<INotifier>()
            .ToList();

        var missingChannels = selectedChannels
            .Where(c => !notifiersByChannel.ContainsKey(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (activeNotifiers.Count == 0 && missingChannels.Count == 0)
        {
            logger.LogWarning("No channels selected. Notification will be persisted without recipients.");
        }
        else if (missingChannels.Count > 0)
        {
            logger.LogWarning(
                "No active notifiers for channels: {Channels}. Recipients will be marked Failed.",
                string.Join(",", missingChannels));
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
        var recipients = new List<NotificationRecipient>();
        foreach (var adminUserId in adminUserIds)
        {
            foreach (var notifier in activeNotifiers)
            {
                recipients.Add(new NotificationRecipient
                {
                    NotificationId = notificationId,
                    AdminUserId = adminUserId,
                    DeliveryChannel = notifier.Channel,
                    DeliveryStatus = DeliveryStatus.Pending,
                    CreateDate = now,
                    LastUpdate = now
                });
            }

            foreach (var channel in missingChannels)
            {
                recipients.Add(new NotificationRecipient
                {
                    NotificationId = notificationId,
                    AdminUserId = adminUserId,
                    DeliveryChannel = channel,
                    DeliveryStatus = DeliveryStatus.Failed,
                    DeliveryError = TruncateError($"Channel notifier '{channel}' is not registered."),
                    CreateDate = now,
                    LastUpdate = now
                });
            }
        }

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
                .SetProperty(r => r.DeliveryError, (string?)null)
                .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
            ct: ct
        );
    }

    public Task MarkRead(int notificationId, int adminUserId, CancellationToken ct = default)
    {
        // Mark as read without overwriting Failed delivery status (keep DeliveryError visible in UI).
        return notificationRecipientCommandServices.UpdateWhere(
            predicate: r => r.NotificationId == notificationId
                            && r.AdminUserId == adminUserId,
            set: calls => calls
                .SetProperty(r => r.ReadAt, DateTimeOffset.UtcNow)
                .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
            ct: ct
        );
    }

    public async Task<GetAllNotificationsResponse> GetAllForUserAsync(int adminUserId, CancellationToken ct = default)
    {
        var rows = await notificationRecipientQueryService.GetNotificationListByAdminUserIdAsync(adminUserId, ct);
        var items = await MapItemsAsync(adminUserId, rows, ct);
        return new GetAllNotificationsResponse { Notifications = items };
    }

    public async Task<GetNotificationsResponse> GetPageForUserAsync(int adminUserId, GetNotificationsRequest request, CancellationToken ct = default)
    {
        var paged = await notificationRecipientQueryService.GetNotificationListPageByAdminUserIdAsync(adminUserId, request, ct);
        var items = await MapItemsAsync(adminUserId, paged.Items, ct);
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
                .SetProperty(r => r.ReadAt, DateTimeOffset.UtcNow)
                .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
            ct: ct
        );
    }

    private async Task<List<NotificationItemDto>> MapItemsAsync(
        int adminUserId,
        IReadOnlyList<NotificationListRow> rows,
        CancellationToken ct)
    {
        var notificationIds = rows.Select(r => r.Id).ToList();
        var deliveries = await notificationRecipientQueryService
            .GetDeliveriesByAdminUserIdAndNotificationIdsAsync(adminUserId, notificationIds, ct);

        var byNotification = deliveries
            .GroupBy(d => d.NotificationId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(d => new NotificationDeliveryDto
                {
                    Channel = d.Channel,
                    Status = d.Status,
                    Error = d.Error,
                    DeliveredAt = d.DeliveredAt
                }).ToList());

        return rows.Select(r => new NotificationItemDto
        {
            Id = r.Id,
            Type = r.Type,
            Severity = (NotificationSeverity)r.Severity,
            Title = r.Title,
            Message = r.Message,
            IsRead = r.IsRead,
            CreatedAt = r.CreatedAt,
            ReadAt = r.ReadAt,
            Deliveries = byNotification.TryGetValue(r.Id, out var list) ? list : []
        }).ToList();
    }

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
                    .SetProperty(r => r.DeliveryError, (string?)null)
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
        catch (NotificationChannelSkippedException ex)
        {
            // Expected when the admin has no linked channel identity — do not LogError (Wazuh noise).
            var error = TruncateError(ex.Message);
            await notificationRecipientCommandServices.UpdateWhere(
                predicate: r => r.NotificationId == notification.Id
                                && r.AdminUserId == adminUserId
                                && r.DeliveryChannel == notifier.Channel,
                set: calls => calls
                    .SetProperty(r => r.DeliveryStatus, DeliveryStatus.Failed)
                    .SetProperty(r => r.DeliveryError, error)
                    .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
                ct: ct
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send NotificationId={NotificationId} via {Channel} to AdminId={AdminId}",
                notification.Id, notifier.Channel, adminUserId);

            var error = TruncateError(ex.Message);
            await notificationRecipientCommandServices.UpdateWhere(
                predicate: r => r.NotificationId == notification.Id
                                && r.AdminUserId == adminUserId
                                && r.DeliveryChannel == notifier.Channel,
                set: calls => calls
                    .SetProperty(r => r.DeliveryStatus, DeliveryStatus.Failed)
                    .SetProperty(r => r.DeliveryError, error)
                    .SetProperty(r => r.LastUpdate, DateTimeOffset.UtcNow),
                ct: ct
            );
        }
    }

    private static string TruncateError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "Unknown delivery error.";

        var trimmed = error.Trim();
        return trimmed.Length <= MaxDeliveryErrorLength
            ? trimmed
            : trimmed[..MaxDeliveryErrorLength];
    }
}
