using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;

public class QuotaPlanAllowedServerQueryService(
    IQueryService<QuotaPlanAllowedServer, int> q) : IQuotaPlanAllowedServerQueryService
{
    public Task<List<QuotaPlanAllowedServer>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<QuotaPlanAllowedServer?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<QuotaPlanAllowedServer?> GetByQuotaPlanIdAndServerId(int quotaPlanId, int vpnServerId,
        CancellationToken ct)
        => q.FirstOrDefault(
            predicate: x => x.QuotaPlanId == quotaPlanId && x.VpnServerId == vpnServerId,
            orderBy: s => s.OrderBy(x => x.Id),
            asNoTracking: true,
            ct: ct);
    public Task<IPagedResult<QuotaPlanAllowedServer>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}