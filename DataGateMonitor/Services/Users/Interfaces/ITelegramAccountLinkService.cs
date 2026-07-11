using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users.Interfaces;

public interface ITelegramAccountLinkService
{
    /// <summary>
    /// App user (Google/local) requests a code. When <paramref name="telegramId"/> is null,
    /// the user enters the code in the Telegram bot (recommended for mobile).
    /// </summary>
    Task<RequestTelegramAccountLinkCodeResponse> RequestLinkCodeAsync(
        int userId,
        long? telegramId,
        CancellationToken ct);

    /// <summary>Bot requests a code for a Telegram user; the user enters it in the app.</summary>
    Task<RequestTelegramAccountLinkCodeResponse> RequestLinkCodeFromBotAsync(
        long telegramId,
        CancellationToken ct);

    /// <summary>App user submits a code issued by the bot.</summary>
    Task<CompleteTelegramAccountLinkResponse> CompleteLinkFromAppAsync(
        int userId,
        string code,
        CancellationToken ct);

    /// <summary>Telegram bot submits a code issued by the app.</summary>
    Task<CompleteTelegramAccountLinkResponse> CompleteLinkByCodeAsync(
        string code,
        long telegramId,
        CancellationToken ct);
}
