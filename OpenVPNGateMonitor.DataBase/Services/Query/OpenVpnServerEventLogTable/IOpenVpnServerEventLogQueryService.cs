using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;

public interface IOpenVpnServerEventLogQueryService
{
    Task<List<OpenVpnServerEventLog>> GetAllAsync(CancellationToken ct);
    Task<OpenVpnServerEventLog?> GetByIdAsync(int id, CancellationToken ct);

    Task<PagedResult<OpenVpnServerEventLog>> GetByVpnServerIdAsync(int vpnServerId, int page, int pageSize, CancellationToken ct);

    Task<PagedResult<OpenVpnServerEventLog>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}