using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerStatusLogTable;

public class OpenVpnServerStatusLogQueryService(IQueryService<OpenVpnServerStatusLog, int> q) : IOpenVpnServerStatusLogQueryService
{
    public Task<List<OpenVpnServerStatusLog>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<List<OpenVpnServerStatusLog>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.Where(x => x.VpnServerId == vpnServerId, ct: ct);

    public Task<OpenVpnServerStatusLog?> GetBySessionIdAndVpnServerId(Guid sessionId, int vpnServerId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.VpnServerId == vpnServerId, ct);

    public Task<OpenVpnServerStatusLog?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);
    
    public Task<OpenVpnServerStatusLog?> GetByIdAndVpnServerId(int id, int vpnServerId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.Id == id && x.VpnServerId == vpnServerId, ct);
    
    public Task<IPagedResult<OpenVpnServerStatusLog>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}