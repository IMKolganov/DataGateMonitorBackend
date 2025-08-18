using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;

public class TelegramUserLanguagePreferenceQueryService(IQueryService<TelegramUserLanguagePreference, int> q) : ITelegramUserLanguagePreferenceQueryService
{
    public Task<List<TelegramUserLanguagePreference>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<TelegramUserLanguagePreference?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<TelegramUserLanguagePreference?> GetByTelegramId(long telegramId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.TelegramId == telegramId, ct);

    public Task<bool> AnyByTelegramId(long telegramId, CancellationToken ct)
        => q.AnyAsync(x => x.TelegramId == telegramId, ct: ct);

    public Task<IPagedResult<TelegramUserLanguagePreference>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}