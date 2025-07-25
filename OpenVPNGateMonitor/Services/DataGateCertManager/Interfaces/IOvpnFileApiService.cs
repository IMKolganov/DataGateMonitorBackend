using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface IOvpnFileApiService
{
    Task<IssuedOvpnFile> GetOvpnFileByTokenAsync(string token, CancellationToken cancellationToken
        , bool isRevoked = false);
    Task<List<IssuedOvpnFile>> GetAllOvpnFilesAsync(int vpnServerId, CancellationToken cancellationToken);
    Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> GetAllOvpnFilesWithTokenAsync(int vpnServerId,
        CancellationToken cancellationToken, bool isRevoked = false);

    Task<List<IssuedOvpnFile>> GetAllByExternalIdOvpnFilesAsync(int vpnServerId, string externalId,
        CancellationToken cancellationToken, bool isRevoked = false);
    Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> GetAllByExternalIdOvpnFilesWithTokenAsync(int vpnServerId, 
        string externalId, CancellationToken cancellationToken, bool isRevoked = false);
    
    Task<IssuedOvpnFile> AddOvpnFileAsync(AddClientOvpnFileRequest request, 
        CancellationToken cancellationToken);
    Task<(IssuedOvpnFile File, IssuedOvpnFileToken Token)> AddOvpnFileWithTokenAsync(AddClientOvpnFileRequest request, 
        CancellationToken cancellationToken);
    
    Task<IssuedOvpnFile> RevokeOvpnFileAsync(RevokeClientOvpnFileRequest request,
        CancellationToken cancellationToken);

    Task<DownloadOvpnFileResponse> DownloadOvpnFileAsync(DownloadClientOvpnFileRequest request,
        CancellationToken cancellationToken, bool isRevoked = false);
}