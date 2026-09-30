using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.Services.Users;

namespace DataGateMonitor.Services.Others;

public class TelegramNotifier(
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    ITelegramDirectMessageSender telegramDirectMessageSender,
    ILogger<TelegramNotifier> logger)
    : INotifier
{
    public string Channel => "telegram";

    public async Task Send(Notification notification, int adminUserId, CancellationToken ct)
    {
        var links = await userIdentityLinkQueryService.GetListByUserId(adminUserId, ct);
        var telegramId = FreeTierAccessComplianceService.TryGetTelegramId(links);
        if (telegramId is null)
        {
            logger.LogDebug(
                "Admin {AdminUserId} has no linked Telegram; skip telegram channel for NotificationId={NotificationId}",
                adminUserId,
                notification.Id);
            return;
        }

        var text = $"{notification.Title}\n{notification.Message}";
        var sent = await telegramDirectMessageSender.TrySendMessageAsync(telegramId.Value, text, ct);
        if (!sent)
        {
            throw new InvalidOperationException(
                $"Failed to deliver notification {notification.Id} to Telegram chat {telegramId.Value} for admin {adminUserId}.");
        }
    }
}
