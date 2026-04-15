using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;

public interface IQuotaPlanAllowedServerQueryService
{
    /// <summary>Distinct VPN server ids that have at least one quota-plan assignment.</summary>
    Task<HashSet<int>> GetDistinctVpnServerIds(CancellationToken ct);

    /// <summary>VPN server ids allowed for the given quota plan.</summary>
    Task<HashSet<int>> GetVpnServerIdsByQuotaPlanId(int quotaPlanId, CancellationToken ct);

    Task<List<QuotaPlanAllowedServer>> GetAll(CancellationToken ct);
    Task<QuotaPlanAllowedServer?> GetById(int id, CancellationToken ct);
    Task<QuotaPlanAllowedServer?> GetByQuotaPlanIdAndServerId(int quotaPlanId, int vpnServerId,
        CancellationToken ct);
    Task<List<QuotaPlanAllowedServer>> GetListByQuotaPlanId(int quotaPlanId, CancellationToken ct);
    Task<List<QuotaPlanAllowedServer>> GetListByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<IPagedResult<QuotaPlanAllowedServer>> GetPage(int page, int pageSize, int? quotaPlanId, int? vpnServerId, CancellationToken ct);
}