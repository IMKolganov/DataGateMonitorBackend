using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerQueryService
{
    Task<List<OpenVpnServer>> GetAll(bool includeDeleted = false, CancellationToken ct = default);

    Task<OpenVpnServer?> GetById(int id, CancellationToken ct = default);

    Task<List<OpenVpnServer>> GetDefaultExcept(int exceptId, CancellationToken ct = default);

    Task<IPagedResult<OpenVpnServer>> GetPage(
        int page,
        int pageSize,
        bool includeDeleted = false,
        CancellationToken ct = default);

    Task<bool> AnyByServerName(string serverName, CancellationToken ct = default);

    Task<bool> AnyByServerNameExceptId(string serverName, int id, CancellationToken ct = default);
}