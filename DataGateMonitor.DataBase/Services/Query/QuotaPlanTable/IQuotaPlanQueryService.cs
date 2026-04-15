using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.QuotaPlanTable;

public interface IQuotaPlanQueryService
{
    Task<List<QuotaPlan>> GetAll(CancellationToken ct);
    Task<QuotaPlan?> GetById(int id, CancellationToken ct);
    Task<QuotaPlan?> GetDefault(CancellationToken ct);
    Task<IPagedResult<QuotaPlan>> GetPage(int page, int pageSize, CancellationToken ct);
}