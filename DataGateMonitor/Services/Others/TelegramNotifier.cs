using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.Services.Users;

namespace DataGateMonitor.Services.Others;

public class TelegramNotifier(
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    ITelegramDirectMessageSender telegramDirectMessageSender,
    ILogger<TelegramNotifier> logger) : INotifier
{
    public string Channel => "telegram";

    public async Task Send(Notification notification, int adminUserId, CancellationToken ct)
    {
        var links = await userIdentityLinkQueryService.GetListByUserId(adminUserId, ct);
        var telegramId = FreeTierAccessComplianceService.TryGetTelegramId(links);
        if (telegramId is null)
        {
            logger.LogWarning(
                "Skipping telegram delivery for AdminId={AdminUserId} NotificationId={NotificationId}: no Telegram identity link",
                adminUserId,
                notification.Id);
            throw new NotificationChannelSkippedException(
                $"Admin user {adminUserId} is not linked to Telegram (no UserIdentityLink with provider=telegram).");
        }

        // Keep the production message shape used since telegram-notifier-admin-alerts:
        // "{Title}\n{Message}" (matches DataGateVPNBot admin DMs).
        var text = $"{notification.Title}\n{notification.Message}";
        var outcome = await telegramDirectMessageSender.SendMessageAsync(telegramId.Value, text, ct);
        if (!outcome.Success)
        {
            logger.LogWarning(
                "Telegram notify failed for admin {AdminUserId} chat {TelegramId} NotificationId={NotificationId}: {Error}",
                adminUserId,
                telegramId.Value,
                notification.Id,
                outcome.ErrorMessage);
            throw new InvalidOperationException(
                outcome.ErrorMessage
                ?? $"Failed to deliver notification {notification.Id} to Telegram chat {telegramId.Value} for admin {adminUserId}.");
        }
    }
}
