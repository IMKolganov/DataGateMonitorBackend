using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public interface IOpenVpnServerClientQueryService
{
    Task<List<OpenVpnServerClient>> GetAllAsync(CancellationToken ct);
    Task<List<OpenVpnServerClient>> GetAllConnectedByVpnServerIdAsync(int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerClient?> GetByIdAsync(int id, CancellationToken ct);
    Task<OpenVpnServerClient?> GetBySessionAndServerIdAsync(Guid session, int vpnServerId, CancellationToken ct);
    Task<IPagedResult<OpenVpnServerClient>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}