using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.LocalizationTextTable;

public class LocalizationTextQueryService(IQueryService<LocalizationText, int> q) : ILocalizationTextQueryService
{
    public Task<List<LocalizationText>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<LocalizationText?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<string?> GetTextValueByKeyAndLanguage(string key, Language language, CancellationToken ct)
        => q.Query()
            .Where(x => x.Key == key && x.Language == language)
            .Select(x => x.Text)
            .FirstOrDefaultAsync(ct);

    public Task<IPagedResult<LocalizationText>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}