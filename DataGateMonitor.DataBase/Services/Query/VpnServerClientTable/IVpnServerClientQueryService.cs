using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public interface IVpnServerClientQueryService
{
    Task<List<VpnServerClient>> GetAll(CancellationToken ct);
    Task<List<VpnServerClient>> GetAllConnectedByVpnServerId(int vpnServerId, CancellationToken ct);
    /// <summary>All currently connected clients across every VPN server.</summary>
    Task<List<VpnServerClient>> GetAllConnected(CancellationToken ct);
    Task<VpnServerClient?> GetById(int id, CancellationToken ct);
    Task<VpnServerClient?> GetBySessionAndServerId(Guid session, int vpnServerId, CancellationToken ct);
    Task<IPagedResult<VpnServerClient>> GetPage(int page, int pageSize, CancellationToken ct);
}