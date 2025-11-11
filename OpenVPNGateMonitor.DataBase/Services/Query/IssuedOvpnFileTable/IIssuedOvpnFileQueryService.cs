using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;

public interface IIssuedOvpnFileQueryService
{
    Task<List<IssuedOvpnFile>> GetAllAsync(CancellationToken ct);
    Task<List<IssuedOvpnFile>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<List<IssuedOvpnFile>> GetAllByExternalId(string externalId, CancellationToken ct);

    Task<List<IssuedOvpnFile>> GetAllByVpnServerIdAndIsRevoked(int vpnServerId, bool isRevoked, CancellationToken ct);
    Task<List<IssuedOvpnFile>> GetAllByVpnServerIdAndExternalIdAndIsRevoked(int vpnServerId, string externalId,
        bool isRevoked, CancellationToken ct);
    Task<IssuedOvpnFile?> GetByIdAsync(int id, CancellationToken ct);
    Task<IssuedOvpnFile?> GetByIdAndIsRevokedAsync(int id, bool isRevoked, CancellationToken ct);

    Task<IssuedOvpnFile?> GetByIdAndVpnServerIdAndIsRevokedAsync(int id, int vpnServerId, bool isRevoked,
        CancellationToken ct);

    Task<string?> GetExternalIdByCommonName(string commonName, int vpnServerId, CancellationToken ct);
    
    Task<IssuedOvpnFile?> GetByIdAndVpnServerIdAndCommonNameAndIsRevokedAsync(
        int vpnServerId, int ovpnFileId, string commonName, bool isRevoked, CancellationToken ct);
    
    Task<IssuedOvpnFile?> GetByVpnServerIdAndCommonNameAsync(int id, int vpnServerId, string commonName, 
        CancellationToken ct);
    
    Task<bool> ExistsActiveByVpnServerIdAndCommonNameAsync(int vpnServerId, string commonName, CancellationToken ct);

    Task<IPagedResult<IssuedOvpnFile>> GetPageAsync(int page, int pageSize, CancellationToken ct);
}