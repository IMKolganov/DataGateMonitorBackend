using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;

namespace DataGateMonitor.Services.TelegramBot.Interfaces;

public interface ITelegramUserService
{
    Task<TelegramBotUser> RegisterUserAsync(TelegramBotUser telegramBotUserRequest, 
        CancellationToken cancellationToken);

    Task<TelegramBotUser> GetUserAsync(long telegramId, CancellationToken cancellationToken);
    Task<List<TelegramBotUser>> GetAdminsAsync(CancellationToken cancellationToken);

    Task<List<TelegramBotUser>> GetAllUsersAsync(CancellationToken ct);

    Task<List<TelegramBotUser>> GetAllUsersAsync(GetAllTelegramBotUsersRequest request, CancellationToken ct);

    Task<TelegramBotUser?> GetUserByTelegramIdAsync(long telegramId, CancellationToken cancellationToken);

    Task<bool> BlockUserAsync(long telegramId, CancellationToken cancellationToken);
    Task<bool> UnblockUserAsync(long telegramId, CancellationToken cancellationToken);
    
    Task<bool> SetAdminAsync(long telegramId, CancellationToken cancellationToken);
    Task<bool> UnsetAdminAsync(long telegramId, CancellationToken cancellationToken);
}