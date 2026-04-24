using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;

public class TelegramUserLanguagePreferenceQueryService(IQueryService<TelegramUserLanguagePreference, int> q) : ITelegramUserLanguagePreferenceQueryService
{
    public Task<List<TelegramUserLanguagePreference>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<TelegramUserLanguagePreference?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<TelegramUserLanguagePreference?> GetByTelegramId(long telegramId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.TelegramId == telegramId, ct);

    public Task<bool> AnyByTelegramId(long telegramId, CancellationToken ct)
        => q.Any(x => x.TelegramId == telegramId, ct: ct);

    public Task<IPagedResult<TelegramUserLanguagePreference>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}