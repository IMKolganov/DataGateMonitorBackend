using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerPiHoleConfigTable;

public class VpnServerPiHoleConfigQueryService(IQueryService<VpnServerPiHoleConfig, int> q)
    : IVpnServerPiHoleConfigQueryService
{
    public Task<VpnServerPiHoleConfig?> GetByVpnServerId(int vpnServerId, CancellationToken ct) =>
        q.Query()
            .Where(x => x.VpnServerId == vpnServerId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
}
