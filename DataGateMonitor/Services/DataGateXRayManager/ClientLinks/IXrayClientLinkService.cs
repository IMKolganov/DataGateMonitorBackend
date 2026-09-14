using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;

namespace DataGateMonitor.Services.DataGateXRayManager.ClientLinks;

public interface IXrayClientLinkService
{
    Task<IssuedXrayClientLink> GetByToken(string token, CancellationToken ct, bool isRevoked = false);

    Task<List<IssuedXrayClientLink>> GetAllByExternalId(string externalId, CancellationToken ct);

    Task<List<IssuedXrayClientLink>> GetAllByVpnServerId(int vpnServerId, CancellationToken ct);

    Task<List<(IssuedXrayClientLink File, IssuedXrayClientLinkToken? Token)>> GetAllByVpnServerIdWithToken(
        int vpnServerId, CancellationToken ct, bool isRevoked = false);

    Task<List<IssuedXrayClientLink>> GetAllByExternalIdAndVpnServerId(int vpnServerId, string externalId,
        CancellationToken ct, bool isRevoked = false);

    Task<List<(IssuedXrayClientLink File, IssuedXrayClientLinkToken? Token)>>
        GetAllByExternalIdAndVpnServerIdWithToken(int vpnServerId, string externalId, CancellationToken ct,
            bool isRevoked = false);

    Task<(IssuedXrayClientLink File, IssuedXrayClientLinkToken Token)> AddClientLinkWithToken(
        AddXrayClientLinkRequest request, CancellationToken ct);

    Task<IssuedXrayClientLink> AddClientLink(AddXrayClientLinkRequest request, CancellationToken ct);

    Task<IssuedXrayClientLink> RevokeClientLink(RevokeXrayClientLinkRequest request, CancellationToken ct);

    Task<DownloadXrayClientLinkResponse> DownloadClientLink(DownloadXrayClientLinkRequest request,
        CancellationToken ct, bool isRevoked = false);

    Task<DownloadXrayClientLinkResponse> DownloadClientLinkByCn(DownloadXrayClientLinkByCnRequest request,
        CancellationToken ct, bool isRevoked = false);
}
