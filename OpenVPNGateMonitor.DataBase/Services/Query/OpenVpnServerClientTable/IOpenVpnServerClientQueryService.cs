using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public interface IOpenVpnServerClientQueryService
{
    Task<List<OpenVpnServerClient>> GetAllAsync(CancellationToken ct);
    Task<List<OpenVpnServerClient>> GetAllConnectedByVpnServerIdAsync(int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerClient?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<OpenVpnServerClient>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}