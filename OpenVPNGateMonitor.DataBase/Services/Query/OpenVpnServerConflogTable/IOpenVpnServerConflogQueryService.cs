using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerConflogTable;

public interface IOpenVpnServerConflogQueryService
{
    Task<OpenVpnServerConflog?> GetById(int id, CancellationToken ct = default);
    Task<OpenVpnServerConflog?> GetLastByVpnServerId(int vpnServerId, CancellationToken ct = default);
    Task<OpenVpnServerConflog?> GetLastByRequestUrl(string requestUrl, CancellationToken ct = default);
    Task<IPagedResult<OpenVpnServerConflog>> GetPageByVpnServerId(int vpnServerId, int page, int pageSize, CancellationToken ct = default);
    Task<IPagedResult<OpenVpnServerConflog>> GetPage(int page, int pageSize, CancellationToken ct = default);
}
