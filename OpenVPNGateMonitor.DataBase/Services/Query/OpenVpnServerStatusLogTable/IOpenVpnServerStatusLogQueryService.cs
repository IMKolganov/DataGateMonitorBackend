using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerStatusLogTable;

public interface IOpenVpnServerStatusLogQueryService
{
    Task<List<OpenVpnServerStatusLog>> GetAll(CancellationToken ct);
    Task<List<OpenVpnServerStatusLog>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerStatusLog?> GetByIdAndVpnServerId(int id, int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerStatusLog?> GetBySessionIdAndVpnServerId(Guid session, int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerStatusLog?> GetById(int id, CancellationToken ct);
    Task<IPagedResult<OpenVpnServerStatusLog>> GetPage(int page, int pageSize, CancellationToken ct);
}