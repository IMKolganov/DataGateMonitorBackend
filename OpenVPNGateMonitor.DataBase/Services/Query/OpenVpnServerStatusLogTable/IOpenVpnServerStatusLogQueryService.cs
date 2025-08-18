using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerStatusLogTable;

public interface IOpenVpnServerStatusLogQueryService
{
    Task<List<OpenVpnServerStatusLog>> GetAllAsync(CancellationToken ct);
    Task<List<OpenVpnServerStatusLog>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerStatusLog?> GetByIdAndVpnServerIdAsync(int id, int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerStatusLog?> GetBySessionIdAndVpnServerIdAsync(Guid session, int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerStatusLog?> GetByIdAsync(int id, CancellationToken ct);
    Task<IPagedResult<OpenVpnServerStatusLog>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}