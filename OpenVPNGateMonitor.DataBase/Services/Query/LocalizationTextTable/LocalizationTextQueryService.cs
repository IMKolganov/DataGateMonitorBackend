using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.LocalizationTextTable;

public class LocalizationTextQueryService(IQueryService<LocalizationText, int> q) : ILocalizationTextQueryService
{
    public Task<List<LocalizationText>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<LocalizationText?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<string?> GetTextValueByKeyAndLanguageAsync(string key, Language language, CancellationToken ct)
        => q.Query()
            .Where(x => x.Key == key && x.Language == language)
            .Select(x => x.Text)
            .FirstOrDefaultAsync(ct);

    public Task<IPagedResult<LocalizationText>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}