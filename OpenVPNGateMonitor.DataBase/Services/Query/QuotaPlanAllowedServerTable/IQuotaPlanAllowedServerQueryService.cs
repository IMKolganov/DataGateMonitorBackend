using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;

public interface IQuotaPlanAllowedServerQueryService
{
    Task<List<QuotaPlanAllowedServer>> GetAll(CancellationToken ct);
    Task<QuotaPlanAllowedServer?> GetById(int id, CancellationToken ct);
    Task<QuotaPlanAllowedServer?> GetByQuotaPlanIdAndServerId(int quotaPlanId, int vpnServerId,
        CancellationToken ct);
    Task<IPagedResult<QuotaPlanAllowedServer>> GetPage(int page, int pageSize, CancellationToken ct);
}