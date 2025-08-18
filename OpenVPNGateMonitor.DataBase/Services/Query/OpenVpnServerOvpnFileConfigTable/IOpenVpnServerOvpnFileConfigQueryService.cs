using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;

public interface IOpenVpnServerOvpnFileConfigQueryService
{
    Task<List<OpenVpnServerOvpnFileConfig>> GetAllAsync(CancellationToken ct);
    Task<OpenVpnServerOvpnFileConfig?> GetByIdAsync(int id, CancellationToken ct);
    Task<OpenVpnServerOvpnFileConfig?> GetByVpnServerIdIdAsync(int vpnServerId, CancellationToken ct);
    Task<bool> AnyByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<IPagedResult<OpenVpnServerOvpnFileConfig>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}