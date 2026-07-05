using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users.Interfaces;

public interface ITelegramAccountLinkService
{
    /// <summary>
    /// Dashboard/client user (Google or local login) requests a one-time code to enter in the Telegram bot.
    /// </summary>
    Task<RequestTelegramAccountLinkCodeResponse> RequestLinkCodeAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Telegram bot submits the code; merges the dashboard account into the Telegram-linked user.
    /// </summary>
    Task<CompleteTelegramAccountLinkResponse> CompleteLinkByCodeAsync(
        string code,
        long telegramId,
        CancellationToken ct);
}
