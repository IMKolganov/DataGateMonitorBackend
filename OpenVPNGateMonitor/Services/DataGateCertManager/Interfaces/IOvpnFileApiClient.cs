using OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface IOvpnFileApiClient
{
    Task<OvpnFileMetadata> AddOvpnFileAsync(int vpnServerId, GenerateOvpnFileRequest request, 
        CancellationToken cancellationToken);
    Task<bool> RevokeOvpnFileAsync(int vpnServerId, RevokeOvpnFileRequest request, CancellationToken cancellationToken);
    Task<OvpnFileDownload> DownloadOvpnFileAsync(int vpnServerId, DownloadOvpnFileRequest request, CancellationToken cancellationToken);
}