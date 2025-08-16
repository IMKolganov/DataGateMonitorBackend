using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.LocalizationTextTable;

public interface ILocalizationTextQueryService
{
    Task<List<LocalizationText>> GetAllAsync(CancellationToken ct);
    Task<LocalizationText?> GetByIdAsync(int id, CancellationToken ct);
    Task<string?> GetTextValueByKeyAndLanguageAsync(string key, Language language, CancellationToken ct);
    Task<IPagedResult<LocalizationText>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    
    
}