using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;

public interface IOpenVpnServerOvpnFileConfigQueryService
{
    Task<List<OpenVpnServerOvpnFileConfig>> GetAll(CancellationToken ct);
    Task<OpenVpnServerOvpnFileConfig?> GetById(int id, CancellationToken ct);
    Task<OpenVpnServerOvpnFileConfig?> GetByVpnServerIdId(int vpnServerId, CancellationToken ct);
    Task<bool> AnyByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<IPagedResult<OpenVpnServerOvpnFileConfig>> GetPage(int page, int pageSize, CancellationToken ct);
}