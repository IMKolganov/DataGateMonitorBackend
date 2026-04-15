using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerStatusLogTable;

public interface IVpnServerStatusLogQueryService
{
    Task<List<VpnServerStatusLog>> GetAll(CancellationToken ct);
    Task<List<VpnServerStatusLog>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<VpnServerStatusLog?> GetByIdAndVpnServerId(int id, int vpnServerId, CancellationToken ct);
    Task<VpnServerStatusLog?> GetBySessionIdAndVpnServerId(Guid session, int vpnServerId, CancellationToken ct);
    Task<VpnServerStatusLog?> GetById(int id, CancellationToken ct);
    Task<IPagedResult<VpnServerStatusLog>> GetPage(int page, int pageSize, CancellationToken ct);
}