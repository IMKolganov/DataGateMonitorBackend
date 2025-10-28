using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;

public class QuotaPlanQueryService(IQueryService<QuotaPlan, int> q) : IQuotaPlanQueryService
{
    public Task<List<QuotaPlan>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<QuotaPlan?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);
    public Task<QuotaPlan?> GetDefaultAsync(CancellationToken ct)
        => q.FirstOrDefaultAsync(
            predicate: x => x.IsDefault && x.IsActive,
            orderBy: s => s.OrderBy(x => x.Id),
            asNoTracking: true,
            ct: ct);
    public Task<IPagedResult<QuotaPlan>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}