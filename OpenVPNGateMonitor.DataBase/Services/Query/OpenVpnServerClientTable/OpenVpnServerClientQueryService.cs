using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public class OpenVpnServerClientQueryService(IQueryService<OpenVpnServerClient, int> q) : IOpenVpnServerClientQueryService
{
    public Task<List<OpenVpnServerClient>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<List<OpenVpnServerClient>> GetAllConnectedByVpnServerIdAsync(int vpnServerId, CancellationToken ct)
        => q.WhereAsync(x => x.IsConnected && x.VpnServerId == vpnServerId, ct: ct);

    public Task<OpenVpnServerClient?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<OpenVpnServerClient?> GetBySessionAndServerIdAsync(Guid session, int vpnServerId, CancellationToken ct)
    => q.Query().AsNoTracking().FirstOrDefaultAsync(
        x=>x.SessionId == session && x.VpnServerId == vpnServerId, ct);

    public Task<PagedResult<OpenVpnServerClient>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}