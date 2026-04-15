using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;

public interface ITelegramUserLanguagePreferenceQueryService
{
    Task<List<TelegramUserLanguagePreference>> GetAll(CancellationToken ct);
    Task<TelegramUserLanguagePreference?> GetById(int id, CancellationToken ct);
    Task<TelegramUserLanguagePreference?> GetByTelegramId(long telegramId, CancellationToken ct);
    Task<bool> AnyByTelegramId(long telegramId, CancellationToken ct);

    Task<IPagedResult<TelegramUserLanguagePreference>> GetPage(int page, int pageSize, CancellationToken ct);
}