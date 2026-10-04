using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.TelegramBot.Interfaces;

namespace DataGateMonitor.Services.Others;

public class TelegramNotifier(
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    ITelegramDirectMessageSender directMessageSender,
    ILogger<TelegramNotifier> logger) : INotifier
{
    public string Channel => "telegram";

    public async Task Send(Notification notification, int adminUserId, CancellationToken ct)
    {
        var links = await userIdentityLinkQueryService.GetListByUserId(adminUserId, ct);
        var telegramLink = links.FirstOrDefault(l =>
            string.Equals(l.Provider, AuthIdentityProviders.Telegram, StringComparison.OrdinalIgnoreCase));

        if (telegramLink is null)
        {
            throw new InvalidOperationException(
                $"Admin user {adminUserId} is not linked to Telegram (no UserIdentityLink with provider=telegram).");
        }

        if (!long.TryParse(telegramLink.ExternalId, out var telegramId) || telegramId <= 0)
        {
            throw new InvalidOperationException(
                $"Admin user {adminUserId} has invalid Telegram ExternalId '{telegramLink.ExternalId}'.");
        }

        var text = FormatMessage(notification);
        var outcome = await directMessageSender.SendMessageAsync(telegramId, text, ct);
        if (!outcome.Success)
        {
            logger.LogWarning(
                "Telegram notify failed for admin {AdminUserId} chat {TelegramId}: {Error}",
                adminUserId,
                telegramId,
                outcome.ErrorMessage);
            throw new InvalidOperationException(outcome.ErrorMessage ?? "Telegram sendMessage failed.");
        }
    }

    private static string FormatMessage(Notification notification)
    {
        var severity = notification.Severity.ToString();
        var title = notification.Title?.Trim() ?? "(no title)";
        var message = notification.Message?.Trim();
        if (string.IsNullOrEmpty(message))
            return $"[{severity}] {title}";

        return $"[{severity}] {title}\n{message}";
    }
}
