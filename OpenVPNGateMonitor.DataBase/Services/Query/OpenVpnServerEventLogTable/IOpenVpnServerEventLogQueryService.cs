using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;

public interface IOpenVpnServerEventLogQueryService
{
    Task<List<OpenVpnServerEventLog>> GetAll(CancellationToken ct);
    Task<OpenVpnServerEventLog?> GetById(int id, CancellationToken ct);

    Task<IPagedResult<OpenVpnServerEventLog>> GetByVpnServerId(int vpnServerId, int page, int pageSize, CancellationToken ct);

    Task<IPagedResult<OpenVpnServerEventLog>> GetPage(int page, int pageSize, CancellationToken ct);
}