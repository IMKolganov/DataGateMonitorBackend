using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.LocalizationTextTable;

public interface ILocalizationTextQueryService
{
    Task<List<LocalizationText>> GetAll(CancellationToken ct);
    Task<LocalizationText?> GetById(int id, CancellationToken ct);
    Task<string?> GetTextValueByKeyAndLanguage(string key, Language language, CancellationToken ct);
    Task<IPagedResult<LocalizationText>> GetPage(int page, int pageSize, CancellationToken ct);
    
    
}