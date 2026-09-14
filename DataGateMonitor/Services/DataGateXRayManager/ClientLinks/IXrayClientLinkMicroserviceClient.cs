using DataGateMonitor.SharedModels.DataGateXRayManager.ClientLink.Requests;
using DataGateMonitor.SharedModels.DataGateXRayManager.ClientLink.Responses;

namespace DataGateMonitor.Services.DataGateXRayManager.ClientLinks;

public interface IXrayClientLinkMicroserviceClient
{
    Task<ClientLinkMetadata> AddClientLink(int vpnServerId, GenerateClientLinkRequest request,
        CancellationToken cancellationToken);

    Task<ClientLinkMetadata> RevokeClientLink(int vpnServerId, RevokeClientLinkRequest request,
        CancellationToken cancellationToken);

    Task<ClientLinkDownload> DownloadClientLink(int vpnServerId, DownloadClientLinkRequest request,
        CancellationToken cancellationToken);
}
