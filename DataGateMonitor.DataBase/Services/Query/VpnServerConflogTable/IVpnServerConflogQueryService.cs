using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerConflogTable;

public interface IVpnServerConflogQueryService
{
    Task<VpnServerConflog?> GetById(int id, CancellationToken ct = default);
    Task<VpnServerConflog?> GetLastByVpnServerId(int vpnServerId, CancellationToken ct = default);
    Task<VpnServerConflog?> GetLastByRequestUrl(string requestUrl, CancellationToken ct = default);
    Task<IPagedResult<VpnServerConflog>> GetPageByVpnServerId(int vpnServerId, int page, int pageSize, CancellationToken ct = default);
    Task<IPagedResult<VpnServerConflog>> GetPage(int page, int pageSize, CancellationToken ct = default);
}
