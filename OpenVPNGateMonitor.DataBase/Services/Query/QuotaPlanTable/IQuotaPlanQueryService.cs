using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;

public interface IQuotaPlanQueryService
{
    Task<List<QuotaPlan>> GetAllAsync(CancellationToken ct);
    Task<QuotaPlan?> GetByIdAsync(int id, CancellationToken ct);
    Task<IPagedResult<QuotaPlan>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}