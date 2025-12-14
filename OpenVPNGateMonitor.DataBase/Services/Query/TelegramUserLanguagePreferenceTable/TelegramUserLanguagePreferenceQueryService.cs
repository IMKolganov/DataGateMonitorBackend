using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;

public class TelegramUserLanguagePreferenceQueryService(IQueryService<TelegramUserLanguagePreference, int> q) : ITelegramUserLanguagePreferenceQueryService
{
    public Task<List<TelegramUserLanguagePreference>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<TelegramUserLanguagePreference?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<TelegramUserLanguagePreference?> GetByTelegramId(long telegramId, CancellationToken ct)
        => q.FirstOrDefault(x => x.TelegramId == telegramId, asNoTracking: true, ct: ct);

    public Task<bool> AnyByTelegramId(long telegramId, CancellationToken ct)
        => q.Any(x => x.TelegramId == telegramId, ct: ct);

    public Task<IPagedResult<TelegramUserLanguagePreference>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}