using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public class OpenVpnServerQueryService(IQueryService<OpenVpnServer, int> q) : IOpenVpnServerQueryService
{
    public Task<List<OpenVpnServer>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<OpenVpnServer?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<List<OpenVpnServer>> GetDefaultExceptAsync(int exceptId, CancellationToken ct)
        => q.WhereAsync(x => x.IsDefault && x.Id != exceptId, ct: ct);

    public Task<IPagedResult<OpenVpnServer>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}