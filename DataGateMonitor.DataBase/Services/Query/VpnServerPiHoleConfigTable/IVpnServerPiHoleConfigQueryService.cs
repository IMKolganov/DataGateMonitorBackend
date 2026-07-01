using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerPiHoleConfigTable;

public interface IVpnServerPiHoleConfigQueryService
{
    Task<VpnServerPiHoleConfig?> GetByVpnServerId(int vpnServerId, CancellationToken ct);
}
