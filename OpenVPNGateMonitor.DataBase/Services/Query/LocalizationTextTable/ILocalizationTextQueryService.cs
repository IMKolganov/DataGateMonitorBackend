using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.LocalizationTextTable;

public interface ILocalizationTextQueryService
{
    Task<List<LocalizationText>> GetAll(CancellationToken ct);
    Task<LocalizationText?> GetById(int id, CancellationToken ct);
    Task<string?> GetTextValueByKeyAndLanguage(string key, Language language, CancellationToken ct);
    Task<IPagedResult<LocalizationText>> GetPage(int page, int pageSize, CancellationToken ct);
    
    
}