using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

public interface IOvpnFileApiClient
{
    Task<OvpnFileMetadata> AddOvpnFile(int vpnServerId, GenerateOvpnFileRequest request, 
        CancellationToken cancellationToken);
    Task<bool> RevokeOvpnFile(int vpnServerId, RevokeOvpnFileRequest request, CancellationToken cancellationToken);
    Task<OvpnFileDownload> DownloadOvpnFile(int vpnServerId, DownloadOvpnFileRequest request, CancellationToken cancellationToken);
}