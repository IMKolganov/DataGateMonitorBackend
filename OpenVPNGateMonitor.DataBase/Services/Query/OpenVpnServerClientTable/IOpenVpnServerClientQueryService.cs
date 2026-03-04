using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;

public interface IOpenVpnServerClientQueryService
{
    Task<List<OpenVpnServerClient>> GetAll(CancellationToken ct);
    Task<List<OpenVpnServerClient>> GetAllConnectedByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<OpenVpnServerClient?> GetById(int id, CancellationToken ct);
    Task<OpenVpnServerClient?> GetBySessionAndServerId(Guid session, int vpnServerId, CancellationToken ct);
    Task<IPagedResult<OpenVpnServerClient>> GetPage(int page, int pageSize, CancellationToken ct);
}