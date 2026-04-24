namespace DataGateMonitor.Services.DataGateXRayManager.ClientLinks;

public interface IXrayClientLinkMicroserviceClient
{
    Task<ClientLinkMetadataDto> AddClientLink(int vpnServerId, GenerateClientLinkMicroserviceRequest request,
        CancellationToken cancellationToken);

    Task<ClientLinkMetadataDto> RevokeClientLink(int vpnServerId, RevokeClientLinkMicroserviceRequest request,
        CancellationToken cancellationToken);

    Task<ClientLinkDownloadDto> DownloadClientLink(int vpnServerId, DownloadClientLinkMicroserviceRequest request,
        CancellationToken cancellationToken);
}
