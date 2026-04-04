using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public interface IOpenVpnServerQueryService
{
    /// <param name="requireQuotaPlanAssignment">When true, only servers present in <c>QuotaPlanAllowedServers</c> are returned. Ignored when <paramref name="restrictToQuotaPlanId"/> is set.</param>
    /// <param name="restrictToQuotaPlanId">When set, only servers linked to this quota plan in <c>QuotaPlanAllowedServers</c> are returned.</param>
    Task<List<OpenVpnServer>> GetAll(bool includeDeleted = false, bool requireQuotaPlanAssignment = false,
        int? restrictToQuotaPlanId = null, CancellationToken ct = default);

    Task<OpenVpnServer?> GetById(int id, CancellationToken ct = default);

    Task<List<OpenVpnServer>> GetDefaultExcept(int exceptId, CancellationToken ct = default);

    Task<IPagedResult<OpenVpnServer>> GetPage(
        int page,
        int pageSize,
        bool includeDeleted = false,
        bool requireQuotaPlanAssignment = false,
        int? restrictToQuotaPlanId = null,
        CancellationToken ct = default);

    Task<bool> AnyByServerName(string serverName, CancellationToken ct = default);

    Task<bool> AnyByServerNameExceptId(string serverName, int id, CancellationToken ct = default);
}