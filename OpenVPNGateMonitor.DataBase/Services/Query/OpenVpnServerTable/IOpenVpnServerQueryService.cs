using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerQueryService
{
    Task<List<OpenVpnServer>> GetAll(CancellationToken ct);
    Task<OpenVpnServer?> GetById(int id, CancellationToken ct);
    Task<List<OpenVpnServer>> GetDefaultExcept(int exceptId, CancellationToken ct);
    Task<IPagedResult<OpenVpnServer>> GetPage(int page, int pageSize, CancellationToken ct);
}