namespace DataGateMonitor.Services.Users.Interfaces;

public interface IFreeTierAccessComplianceService
{
    /// <summary>
    /// When the user has an active Free/Default plan, they must be a merged Telegram+Google account
    /// or subscribed to the required Telegram channel. Otherwise admins are notified.
    /// </summary>
    /// <param name="isChannelSubscribed">When set by the bot after a live getChatMember check.</param>
    Task<FreeTierAccessComplianceResult> AuditAndNotifyIfNeededAsync(
        int userId,
        string context,
        bool? isChannelSubscribed = null,
        CancellationToken ct = default);

    Task<FreeTierAccessComplianceResult> AuditAndNotifyIfNeededByTelegramIdAsync(
        long telegramId,
        string context,
        bool? isChannelSubscribed = null,
        CancellationToken ct = default);
}
