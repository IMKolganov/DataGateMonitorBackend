using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;

public interface ITelegramUserLanguagePreferenceQueryService
{
    Task<List<TelegramUserLanguagePreference>> GetAllAsync(CancellationToken ct);
    Task<TelegramUserLanguagePreference?> GetByIdAsync(int id, CancellationToken ct);
    Task<TelegramUserLanguagePreference?> GetByTelegramId(long telegramId, CancellationToken ct);
    Task<bool> AnyByTelegramId(long telegramId, CancellationToken ct);

    Task<IPagedResult<TelegramUserLanguagePreference>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}