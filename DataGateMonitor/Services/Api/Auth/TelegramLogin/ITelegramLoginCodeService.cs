using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.TelegramLogin;

public interface ITelegramLoginCodeService
{
    /// <summary>
    /// Generates a one-time login code for the given Telegram user. Called by the bot.
    /// TelegramBotUser must already exist for this TelegramId.
    /// </summary>
    Task<TelegramRequestLoginCodeResponse?> RequestLoginCodeAsync(
        TelegramRequestLoginCodeRequest request,
        CancellationToken ct);

    /// <summary>
    /// Exchanges the code from the bot for dashboard tokens. Called by the dashboard when user enters the code.
    /// </summary>
    Task<LoginResponse> LoginWithCodeAsync(TelegramCodeLoginRequest request, CancellationToken ct);
}
