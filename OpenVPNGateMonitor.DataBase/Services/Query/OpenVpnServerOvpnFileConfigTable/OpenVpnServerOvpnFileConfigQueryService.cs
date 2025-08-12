using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;

public class OpenVpnServerOvpnFileConfigQueryService(
    IQueryService<OpenVpnServerOvpnFileConfig, int> q) : IOpenVpnServerOvpnFileConfigQueryService
{
    public Task<List<OpenVpnServerOvpnFileConfig>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);
    public Task<OpenVpnServerOvpnFileConfig?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);
    public Task<OpenVpnServerOvpnFileConfig?> GetByVpnServerIdIdAsync(int vpnServerId, CancellationToken ct)
        => q.Query().FirstOrDefaultAsync(x => x.VpnServerId == vpnServerId, ct);
    
    public Task<bool> AnyByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.AnyAsync(x => x.VpnServerId == vpnServerId, ct: ct);
    public Task<PagedResult<OpenVpnServerOvpnFileConfig>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}