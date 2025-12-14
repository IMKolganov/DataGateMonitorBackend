using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public class OpenVpnServerQueryService(IQueryService<OpenVpnServer, int> q) : IOpenVpnServerQueryService
{
    public Task<List<OpenVpnServer>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<OpenVpnServer?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<List<OpenVpnServer>> GetDefaultExcept(int exceptId, CancellationToken ct)
        => q.Where(x => x.IsDefault && x.Id != exceptId, ct: ct);

    public Task<IPagedResult<OpenVpnServer>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}