namespace DataGateMonitor.Services.TelegramBot.Interfaces;

public interface ITelegramChannelMembershipChecker
{
    Task<bool> IsSubscribedAsync(long telegramUserId, CancellationToken ct);
}
