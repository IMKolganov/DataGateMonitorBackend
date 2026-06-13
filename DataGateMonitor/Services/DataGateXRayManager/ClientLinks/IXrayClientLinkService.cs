using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;

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

    Task<(IssuedXrayClientLink File, IssuedXrayClientLinkToken Token)> AddClientLinkWithToken(AddFileRequest request,
        CancellationToken ct);

    Task<IssuedXrayClientLink> AddClientLink(AddFileRequest request, CancellationToken ct);

    Task<IssuedXrayClientLink> RevokeClientLink(RevokeFileRequest request, CancellationToken ct);

    Task<DownloadFileResponse> DownloadClientLink(DownloadFileRequest request, CancellationToken ct,
        bool isRevoked = false);

    Task<DownloadFileResponse> DownloadClientLinkByCn(DownloadFileByCnRequest request, CancellationToken ct,
        bool isRevoked = false);
}
