using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;

public interface IIssuedOvpnFileQueryService
{
    Task<List<IssuedOvpnFile>> GetAll(CancellationToken ct);
    Task<List<IssuedOvpnFile>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<List<IssuedOvpnFile>> GetAllByExternalId(string externalId, CancellationToken ct);

    Task<List<IssuedOvpnFile>> GetAllByVpnServerIdAndIsRevoked(int vpnServerId, bool isRevoked, CancellationToken ct);
    Task<List<IssuedOvpnFile>> GetAllByVpnServerIdAndExternalIdAndIsRevoked(int vpnServerId, string externalId,
        bool isRevoked, CancellationToken ct);
    Task<IssuedOvpnFile?> GetById(int id, CancellationToken ct);
    Task<IssuedOvpnFile?> GetByIdAndIsRevoked(int id, bool isRevoked, CancellationToken ct);

    Task<IssuedOvpnFile?> GetByIdAndVpnServerIdAndIsRevoked(int id, int vpnServerId, bool isRevoked,
        CancellationToken ct);

    Task<string?> GetExternalIdByCommonName(string commonName, int vpnServerId, CancellationToken ct);
    
    Task<IssuedOvpnFile?> GetByIdAndVpnServerIdAndCommonNameAndIsRevoked(
        int vpnServerId, int ovpnFileId, string commonName, bool isRevoked, CancellationToken ct);
    
    Task<IssuedOvpnFile?> GetByVpnServerIdAndCommonName(int id, int vpnServerId, string commonName, 
        CancellationToken ct);
    
    Task<bool> ExistsActiveByVpnServerIdAndCommonName(int vpnServerId, string commonName, CancellationToken ct);

    Task<IPagedResult<IssuedOvpnFile>> GetPage(int page, int pageSize, CancellationToken ct);
}