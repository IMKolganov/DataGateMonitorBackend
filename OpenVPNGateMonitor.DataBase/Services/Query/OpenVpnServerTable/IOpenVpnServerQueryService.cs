using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerQueryService
{
    Task<List<OpenVpnServer>> GetAllAsync(CancellationToken ct);
    Task<OpenVpnServer?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<OpenVpnServer>> GetDefaultExceptAsync(int exceptId, CancellationToken ct);
    Task<IPagedResult<OpenVpnServer>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}