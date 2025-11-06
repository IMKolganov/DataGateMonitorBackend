using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

public interface IOvpnFileApiClient
{
    Task<OvpnFileMetadata> AddOvpnFileAsync(int vpnServerId, GenerateOvpnFileRequest request, 
        CancellationToken cancellationToken);
    Task<bool> RevokeOvpnFileAsync(int vpnServerId, RevokeOvpnFileRequest request, CancellationToken cancellationToken);
    Task<OvpnFileDownload> DownloadOvpnFileAsync(int vpnServerId, DownloadOvpnFileRequest request, CancellationToken cancellationToken);
}