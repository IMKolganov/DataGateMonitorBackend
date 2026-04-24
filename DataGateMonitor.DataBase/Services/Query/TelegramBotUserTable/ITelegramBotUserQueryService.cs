using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.TelegramBotUserTable;

public interface ITelegramBotUserQueryService
{
    Task<List<TelegramBotUser>> GetAll(CancellationToken ct);
    Task<List<TelegramBotUser>> GetAllAdmins(CancellationToken ct);
    Task<TelegramBotUser?> GetById(int id, CancellationToken ct);
    Task<bool> AnyByTelegramId(long telegramId, CancellationToken ct);
    Task<TelegramBotUser?> GetByTelegramId(long telegramId, CancellationToken ct);
    Task<IPagedResult<TelegramBotUser>> GetPage(int page, int pageSize, CancellationToken ct);
}