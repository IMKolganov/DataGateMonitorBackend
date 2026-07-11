using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public class VpnServerClientQueryService(IQueryService<VpnServerClient, int> q) : IVpnServerClientQueryService
{
    public Task<List<VpnServerClient>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<List<VpnServerClient>> GetAllConnectedByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.Where(x => x.IsConnected && x.VpnServerId == vpnServerId, ct: ct);

    public Task<List<VpnServerClient>> GetAllConnected(CancellationToken ct)
        => q.Where(x => x.IsConnected, ct: ct);

    public Task<VpnServerClient?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<VpnServerClient?> GetBySessionAndServerId(Guid session, int vpnServerId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.SessionId == session && x.VpnServerId == vpnServerId, ct);

    public Task<IPagedResult<VpnServerClient>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}