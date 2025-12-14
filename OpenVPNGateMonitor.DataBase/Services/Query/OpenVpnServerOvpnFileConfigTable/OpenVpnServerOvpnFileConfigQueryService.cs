using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;

public class OpenVpnServerOvpnFileConfigQueryService(
    IQueryService<OpenVpnServerOvpnFileConfig, int> q) : IOpenVpnServerOvpnFileConfigQueryService
{
    public Task<List<OpenVpnServerOvpnFileConfig>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);
    public Task<OpenVpnServerOvpnFileConfig?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);
    public Task<OpenVpnServerOvpnFileConfig?> GetByVpnServerIdId(
        int vpnServerId,
        CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.VpnServerId == vpnServerId, ct);
    
    public Task<bool> AnyByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.Any(x => x.VpnServerId == vpnServerId, ct: ct);
    public Task<IPagedResult<OpenVpnServerOvpnFileConfig>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}