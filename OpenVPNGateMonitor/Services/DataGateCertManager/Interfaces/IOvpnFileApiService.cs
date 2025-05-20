using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface IOvpnFileApiService
{
    Task<List<IssuedOvpnFile>> GetAllOvpnFilesAsync(int vpnServerId, CancellationToken cancellationToken);

    Task<List<IssuedOvpnFile>> GetAllByExternalIdOvpnFilesAsync(int vpnServerId, string externalId,
        CancellationToken cancellationToken);
    
    Task<IssuedOvpnFile> AddOvpnFileAsync(AddOvpnFileRequest request, 
        CancellationToken cancellationToken);

    Task<IssuedOvpnFile> RevokeOvpnFileAsync(RevokeOvpnFileRequest request,
        CancellationToken cancellationToken);

    Task<DownloadOvpnFileResponse> DownloadOvpnFileAsync(DownloadOvpnFileRequest request,
        CancellationToken cancellationToken);
}