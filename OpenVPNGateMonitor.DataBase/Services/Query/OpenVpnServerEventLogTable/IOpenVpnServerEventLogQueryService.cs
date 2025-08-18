using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;

public interface IOpenVpnServerEventLogQueryService
{
    Task<List<OpenVpnServerEventLog>> GetAllAsync(CancellationToken ct);
    Task<OpenVpnServerEventLog?> GetByIdAsync(int id, CancellationToken ct);

    Task<IPagedResult<OpenVpnServerEventLog>> GetByVpnServerIdAsync(int vpnServerId, int page, int pageSize, CancellationToken ct);

    Task<IPagedResult<OpenVpnServerEventLog>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}