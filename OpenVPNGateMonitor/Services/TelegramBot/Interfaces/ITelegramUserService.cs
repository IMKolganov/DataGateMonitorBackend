using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.TelegramBot.Interfaces;

public interface ITelegramUserService
{
    Task<TelegramBotUser> RegisterUserAsync(TelegramBotUser telegramBotUserRequest, 
        CancellationToken cancellationToken);

    Task<List<TelegramBotUser>?> GetAdminsAsync(CancellationToken cancellationToken);

    Task<List<TelegramBotUser>?> GetAllUsersAsync(CancellationToken cancellationToken);
}