using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;

public interface ITelegramBotUserQueryService
{
    Task<List<TelegramBotUser>> GetAllAsync(CancellationToken ct);
    Task<List<TelegramBotUser>> GetAllAdminsAsync(CancellationToken ct);
    Task<TelegramBotUser?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> AnyByTelegramIdAsync(long telegramId, CancellationToken ct);
    Task<TelegramBotUser?> GetByTelegramIdAsync(long telegramId, CancellationToken ct);
    Task<PagedResult<TelegramBotUser>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}