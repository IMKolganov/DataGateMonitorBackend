using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;

public class QuotaPlanQueryService(IQueryService<QuotaPlan, int> q) : IQuotaPlanQueryService
{
    public Task<List<QuotaPlan>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<QuotaPlan?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);
    public Task<QuotaPlan?> GetDefault(CancellationToken ct)
        => q.FirstOrDefault(
            predicate: x => x.IsDefault && x.IsActive,
            orderBy: s => s.OrderBy(x => x.Id),
            asNoTracking: true,
            ct: ct);
    public Task<IPagedResult<QuotaPlan>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}