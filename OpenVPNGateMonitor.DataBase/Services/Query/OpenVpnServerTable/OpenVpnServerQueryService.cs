using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public class OpenVpnServerQueryService(IQueryService<OpenVpnServer, int> q) : IOpenVpnServerQueryService
{
    public Task<List<OpenVpnServer>> GetAll(bool includeDeleted = false, CancellationToken ct = default)
        => includeDeleted
            ? q.GetAll(ct: ct)
            : q.Where(x => !x.IsDeleted, ct: ct);

    public Task<OpenVpnServer?> GetById(int id, CancellationToken ct = default)
        => q.FindById(id, ct: ct);

    public Task<List<OpenVpnServer>> GetDefaultExcept(int exceptId, CancellationToken ct = default)
        => q.Where(x => x.IsDefault && x.Id != exceptId && !x.IsDeleted, ct: ct);

    public Task<IPagedResult<OpenVpnServer>> GetPage(int page, int pageSize, bool includeDeleted = false, CancellationToken ct = default)
        => includeDeleted
            ? q.Page(page, pageSize, ct: ct)
            : q.Page(page, pageSize, predicate: x => !x.IsDeleted, ct: ct);
}