using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Others.Models;

namespace OpenVPNGateMonitor.Services.Others;

public class NotificationService(ICommandService<Notification, int> notificationCommandService,
    ITelegramBotUserQueryService telegramBotUserQueryService) : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    
    private readonly Dictionary<string, INotifier> _notifiersByChannel;

    // public NotificationService(
    //     ILogger<NotificationService> logger,
    //     INotificationCommands notificationCommands,
    //     IAdminUsersQuery adminUsersQuery,
    //     IEnumerable<INotifier> notifiers)
    // {
    //     _logger = logger;
    //     _notificationCommands = notificationCommands;
    //     _adminUsersQuery = adminUsersQuery;
    //     _notifiersByChannel = notifiers?.ToDictionary(n => n.Channel, StringComparer.OrdinalIgnoreCase)
    //                          ?? new Dictionary<string, INotifier>(StringComparer.OrdinalIgnoreCase);
    // }

    public async Task<int> NotifyAdminsAsync(
        NotificationRequest request,
        IEnumerable<string>? channels = null,
        CancellationToken ct = default)
    {
        // 1) Resolve recipients (all admins)
        var adminIds = await telegramBotUserQueryService.GetAllAdminsAsync(ct);
        if (adminIds.Count == 0)
        {
            _logger.LogError("No admins found. Skipping notification: {Type} - {Title}", request.Type, request.Title);
            return 0;
        }

        // 2) Resolve channels
        var selectedChannels = (channels?.Where(c => !string.IsNullOrWhiteSpace(c)).ToList())
                               ?? _notifiersByChannel.Keys.ToList();

        var activeNotifiers = selectedChannels
            .Select(c => _notifiersByChannel.TryGetValue(c, out var n) ? n : null)
            .Where(n => n is not null)!
            .ToList();

        if (activeNotifiers.Count == 0)
        {
            _logger.LogWarning("No active notifiers for channels: {Channels}. Notification will be persisted only.", string.Join(",", selectedChannels));
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

        var notificationId = await _notificationCommands.CreateNotificationAsync(notification, ct);

        // 4) Persist recipients (admin × channel)
        var recipients = from adminId in adminIds
                         from notifier in activeNotifiers
                         select (AdminUserId: adminId, Channel: notifier.Channel);

        await _notificationCommands.AddRecipientsAsync(notificationId, recipients, ct);

        // 5) Fan-out sending (best-effort)
        if (activeNotifiers.Count > 0)
        {
            // Ensure Id is set for downstream notifiers
            notification.Id = notificationId;

            var tasks = new List<Task>(adminIds.Count * activeNotifiers.Count);
            foreach (var adminId in adminIds)
            {
                foreach (var notifier in activeNotifiers)
                {
                    tasks.Add(SendSafeAsync(notifier, notification, adminId, ct));
                }
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                // Should not happen because SendSafeAsync handles exceptions, but keep as a guard.
                _logger.LogError(ex, "Unexpected error during fanout for NotificationId={NotificationId}", notificationId);
            }
        }

        return notificationId;
    }

    public Task MarkDeliveredAsync(int notificationId, int adminUserId, string channel, CancellationToken ct = default)
        => _notificationCommands.MarkDeliveredAsync(notificationId, adminUserId, channel, ct);

    public Task MarkReadAsync(int notificationId, int adminUserId, CancellationToken ct = default)
        => _notificationCommands.MarkReadAsync(notificationId, adminUserId, ct);

    // ----------------------
    // Helpers
    // ----------------------
    private async Task SendSafeAsync(Notifier notifier, Notification notification, int adminUserId, CancellationToken ct)
    {
        try
        {
            await notifier.SendAsync(notification, adminUserId, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Send canceled: NotificationId={NotificationId}, AdminId={AdminId}, Channel={Channel}",
                notification.Id, adminUserId, notifier.Channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send NotificationId={NotificationId} via {Channel} to AdminId={AdminId}",
                notification.Id, notifier.Channel, adminUserId);
            // Do not mark delivered here; notifiers or a separate worker can update delivery status if needed.
        }
    }
}
