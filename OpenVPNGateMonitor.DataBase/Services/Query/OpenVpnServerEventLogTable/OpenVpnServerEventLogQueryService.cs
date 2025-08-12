using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;

public class OpenVpnServerEventLogQueryService(IQueryService<OpenVpnServerEventLog, int> q) : IOpenVpnServerEventLogQueryService
{
    public Task<List<OpenVpnServerEventLog>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<OpenVpnServerEventLog?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);
    
    public Task<PagedResult<OpenVpnServerEventLog>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}