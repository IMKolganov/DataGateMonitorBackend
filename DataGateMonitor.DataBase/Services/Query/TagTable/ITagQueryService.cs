using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.TagTable;

public interface ITagQueryService
{
    Task<List<Tag>> GetAll(CancellationToken ct);
    Task<Tag?> GetById(int id, CancellationToken ct);
    Task<Tag?> GetByName(string name, CancellationToken ct);
    Task<IPagedResult<Tag>> GetPage(int page, int pageSize, CancellationToken ct);
}
