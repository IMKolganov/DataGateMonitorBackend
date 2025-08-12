using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerStatusLogTable;

public class OpenVpnServerStatusLogQueryService(IQueryService<OpenVpnServerStatusLog, int> q) : IOpenVpnServerStatusLogQueryService
{
    public Task<List<OpenVpnServerStatusLog>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<List<OpenVpnServerStatusLog>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.WhereAsync(x => x.VpnServerId == vpnServerId, ct: ct);

    public Task<OpenVpnServerStatusLog?> GetBySessionIdAndVpnServerIdAsync(Guid sessionId, int vpnServerId, CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => 
                x.SessionId == sessionId && x.VpnServerId == vpnServerId, ct);

    public Task<OpenVpnServerStatusLog?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);
    
    public Task<OpenVpnServerStatusLog?> GetByIdAndVpnServerIdAsync(int id, int vpnServerId, CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => 
                x.Id == id && x.VpnServerId == vpnServerId, ct);
    
    public Task<PagedResult<OpenVpnServerStatusLog>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}