using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public class OpenVpnServerClientQueryService(IQueryService<OpenVpnServerClient, int> q) : IOpenVpnServerClientQueryService
{
    public Task<List<OpenVpnServerClient>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<List<OpenVpnServerClient>> GetAllConnectedByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.Where(x => x.IsConnected && x.VpnServerId == vpnServerId, ct: ct);

    public Task<OpenVpnServerClient?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<OpenVpnServerClient?> GetBySessionAndServerId(Guid session, int vpnServerId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.SessionId == session && x.VpnServerId == vpnServerId, ct);

    public Task<IPagedResult<OpenVpnServerClient>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}