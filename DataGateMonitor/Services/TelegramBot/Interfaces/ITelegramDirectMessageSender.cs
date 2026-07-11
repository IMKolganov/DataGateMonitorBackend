namespace DataGateMonitor.Services.TelegramBot.Interfaces;

/// <summary>
/// Sends a direct message to a Telegram user's private chat via the Bot API. Used for one-off
/// transactional notifications (e.g. free-tier grace period expired) — not the interactive bot flow,
/// which lives in the separate telegrambot service.
/// </summary>
public interface ITelegramDirectMessageSender
{
    /// <summary>
    /// Attempts to send <paramref name="text"/> to <paramref name="chatId"/>. Never throws — returns
    /// false on any HTTP/Telegram-API failure (e.g. the user blocked the bot, or the bot token is
    /// unconfigured), so callers can fall back to another channel.
    /// </summary>
    Task<bool> TrySendMessageAsync(long chatId, string text, CancellationToken ct = default);
}
