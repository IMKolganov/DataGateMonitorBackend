using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTable;

public class IssuedXrayClientLinkQueryService(IQueryService<IssuedXrayClientLink, int> q) : IIssuedXrayClientLinkQueryService
{
    public Task<List<IssuedXrayClientLink>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.Where(x => x.VpnServerId == vpnServerId, ct: ct);

    public Task<List<IssuedXrayClientLink>> GetAllByExternalId(string externalId, CancellationToken ct)
        => q.Where(x => x.ExternalId == externalId, ct: ct);

    public Task<List<IssuedXrayClientLink>> GetAllByVpnServerIdAndIsRevoked(int vpnServerId, bool isRevoked,
        CancellationToken ct)
        => q.Where(x => x.VpnServerId == vpnServerId && x.IsRevoked == isRevoked, ct: ct);

    public Task<List<IssuedXrayClientLink>> GetAllByVpnServerIdAndExternalIdAndIsRevoked(int vpnServerId,
        string externalId, bool isRevoked, CancellationToken ct)
        => q.Where(x =>
            x.VpnServerId == vpnServerId && x.ExternalId == externalId && x.IsRevoked == isRevoked, ct: ct);

    public Task<IssuedXrayClientLink?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<IssuedXrayClientLink?> GetByIdAndIsRevoked(int id, bool isRevoked, CancellationToken ct)
        => q.Query().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsRevoked == isRevoked, ct);

    public Task<IssuedXrayClientLink?> GetByIdAndVpnServerIdAndIsRevoked(int id, int vpnServerId, bool isRevoked,
        CancellationToken ct)
        => q.Query().AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id && x.VpnServerId == vpnServerId && x.IsRevoked == isRevoked, ct);

    public Task<IssuedXrayClientLink?> GetByCommonNameAndVpnServerIdAndIsRevoked(string commonName, int vpnServerId,
        bool isRevoked, CancellationToken ct)
        => q.Query().AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.CommonName == commonName && x.VpnServerId == vpnServerId && x.IsRevoked == isRevoked, ct);

    public Task<bool> ExistsActiveByVpnServerIdAndCommonName(int vpnServerId, string commonName, CancellationToken ct)
        => q.Query().AsNoTracking().AnyAsync(
            x => x.VpnServerId == vpnServerId && x.CommonName == commonName && !x.IsRevoked, ct);

    public Task<IssuedXrayClientLink?> GetByIdAndVpnServerIdAndCommonNameAndIsRevoked(int vpnServerId, int linkId,
        string commonName, bool isRevoked, CancellationToken ct)
        => q.Query().AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == linkId && x.CommonName == commonName && x.VpnServerId == vpnServerId && x.IsRevoked == isRevoked, ct);

    public Task<IssuedXrayClientLink?> GetByVpnServerIdAndCommonName(int id, int vpnServerId, string commonName,
        CancellationToken ct)
        => q.Query().AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == id && x.CommonName == commonName && x.VpnServerId == vpnServerId, ct);

    public Task<string?> GetExternalIdByCommonName(string commonName, int vpnServerId, CancellationToken ct)
        => q.Query().AsNoTracking()
            .Where(x => x.VpnServerId == vpnServerId && x.CommonName == commonName && !x.IsRevoked)
            .Select(x => x.ExternalId)
            .FirstOrDefaultAsync(ct);
}
