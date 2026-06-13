using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerStatusLogTable;

public class VpnServerStatusLogQueryService(IQueryService<VpnServerStatusLog, int> q) : IVpnServerStatusLogQueryService
{
    public Task<List<VpnServerStatusLog>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<List<VpnServerStatusLog>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.Where(x => x.VpnServerId == vpnServerId, ct: ct);

    public Task<VpnServerStatusLog?> GetBySessionIdAndVpnServerId(Guid sessionId, int vpnServerId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.VpnServerId == vpnServerId, ct);

    public Task<VpnServerStatusLog?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);
    
    public Task<VpnServerStatusLog?> GetByIdAndVpnServerId(int id, int vpnServerId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.Id == id && x.VpnServerId == vpnServerId, ct);
    
    public Task<IPagedResult<VpnServerStatusLog>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}