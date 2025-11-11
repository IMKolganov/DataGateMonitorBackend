using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;

public class IssuedOvpnFileQueryService(IQueryService<IssuedOvpnFile, int> q) : IIssuedOvpnFileQueryService
{
    public Task<List<IssuedOvpnFile>> GetAllAsync(CancellationToken ct)
        => q.GetAllAsync(ct: ct);

    public Task<List<IssuedOvpnFile>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.WhereAsync(x => x.VpnServerId == vpnServerId, ct: ct);
    
    public Task<List<IssuedOvpnFile>> GetAllByExternalId(string externalId, CancellationToken ct)
        => q.WhereAsync(x => x.ExternalId == externalId, ct: ct);

    public Task<List<IssuedOvpnFile>> GetAllByVpnServerIdAndIsRevoked(int vpnServerId, bool isRevoked, 
        CancellationToken ct)
        => q.WhereAsync(x => x.VpnServerId == vpnServerId && x.IsRevoked == isRevoked, ct: ct);

    public Task<List<IssuedOvpnFile>> GetAllByVpnServerIdAndExternalIdAndIsRevoked(int vpnServerId, string externalId,
        bool isRevoked, CancellationToken ct)
        => q.WhereAsync(x => x.VpnServerId == vpnServerId
                                           && x.ExternalId == externalId && x.IsRevoked == isRevoked, ct: ct);

    public Task<IssuedOvpnFile?> GetByIdAsync(int id, CancellationToken ct)
        => q.FindByIdAsync(id, ct: ct);

    public Task<IssuedOvpnFile?> GetByIdAndIsRevokedAsync(int id, bool isRevoked, CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsRevoked == isRevoked, ct);

    public Task<IssuedOvpnFile?> GetByIdAndVpnServerIdAndIsRevokedAsync(int id, int vpnServerId, bool isRevoked, 
        CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => 
                x.Id == id && x.VpnServerId == vpnServerId && x.IsRevoked == isRevoked, ct);
    
    public Task<string?> GetExternalIdByCommonName(string commonName, int vpnServerId, CancellationToken ct) =>
        q.Query()
            .AsNoTracking()
            .Where(x =>
                x.CommonName == commonName &&
                x.VpnServerId == vpnServerId)
            .Select(x => x.ExternalId)
            .FirstOrDefaultAsync(ct);

    public Task<IssuedOvpnFile?> GetByIdAndVpnServerIdAndCommonNameAndIsRevokedAsync(int vpnServerId, int ovpnFileId,
        string commonName, bool isRevoked,
        CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => 
                x.Id == ovpnFileId && x.CommonName == commonName 
                                   && x.VpnServerId == vpnServerId 
                                   && x.IsRevoked == isRevoked, ct);

    public Task<IssuedOvpnFile?> GetByVpnServerIdAndCommonNameAsync(int vpnServerId, string commonName, 
        CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => 
                x.CommonName == commonName && x.VpnServerId == vpnServerId, ct);

    public Task<IssuedOvpnFile?> GetActiveByIdVpnServerAndCommonNameAndIsRevokedAAsync(
        int vpnServerId, int ovpnFileId, string commonName, bool isRevoked, CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.VpnServerId == vpnServerId
                && x.Id == ovpnFileId
                && x.CommonName == commonName
                && x.IsRevoked == isRevoked, ct);
    
    public Task<bool> ExistsActiveByVpnServerIdAndCommonNameAsync(int vpnServerId, string commonName, CancellationToken ct)
        => q.Query()
            .AsNoTracking()
            .AnyAsync(x =>
                x.VpnServerId == vpnServerId
                && x.CommonName == commonName
                && !x.IsRevoked, ct);
    
    public Task<IPagedResult<IssuedOvpnFile>> GetPageAsync(int page, int pageSize, CancellationToken ct)
        => q.PageAsync(page, pageSize, ct: ct);
}