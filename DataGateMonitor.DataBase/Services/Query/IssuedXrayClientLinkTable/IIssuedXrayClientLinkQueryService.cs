using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTable;

public interface IIssuedXrayClientLinkQueryService
{
    Task<List<IssuedXrayClientLink>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct);
    Task<List<IssuedXrayClientLink>> GetAllByExternalId(string externalId, CancellationToken ct);
    Task<List<IssuedXrayClientLink>> GetAllByVpnServerIdAndIsRevoked(int vpnServerId, bool isRevoked, CancellationToken ct);
    Task<List<IssuedXrayClientLink>> GetAllByVpnServerIdAndExternalIdAndIsRevoked(int vpnServerId, string externalId,
        bool isRevoked, CancellationToken ct);

    Task<IssuedXrayClientLink?> GetById(int id, CancellationToken ct);
    Task<IssuedXrayClientLink?> GetByIdAndIsRevoked(int id, bool isRevoked, CancellationToken ct);
    Task<IssuedXrayClientLink?> GetByIdAndVpnServerIdAndIsRevoked(int id, int vpnServerId, bool isRevoked, CancellationToken ct);
    Task<IssuedXrayClientLink?> GetByCommonNameAndVpnServerIdAndIsRevoked(string commonName, int vpnServerId,
        bool isRevoked, CancellationToken ct);

    Task<bool> ExistsActiveByVpnServerIdAndCommonName(int vpnServerId, string commonName, CancellationToken ct);
    Task<IssuedXrayClientLink?> GetByIdAndVpnServerIdAndCommonNameAndIsRevoked(
        int vpnServerId, int linkId, string commonName, bool isRevoked, CancellationToken ct);
    Task<IssuedXrayClientLink?> GetByVpnServerIdAndCommonName(int id, int vpnServerId, string commonName, CancellationToken ct);

    Task<string?> GetExternalIdByCommonName(string commonName, int vpnServerId, CancellationToken ct);
}
