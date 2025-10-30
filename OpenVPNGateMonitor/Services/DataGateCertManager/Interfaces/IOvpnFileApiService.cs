using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface IOvpnFileApiService
{
    Task<IssuedOvpnFile> GetByTokenAsync(string token, CancellationToken cancellationToken
        , bool isRevoked = false);
    Task<List<IssuedOvpnFile>> GetAllByVpnServerIdAsync(int vpnServerId, CancellationToken cancellationToken);
    Task<List<IssuedOvpnFile>> GetAllByExternalIdAsync(string externalId, 
        CancellationToken cancellationToken);

    Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> GetAllByVpnServerIdWithTokenAsync(int vpnServerId,
        CancellationToken cancellationToken, bool isRevoked = false);

    Task<List<IssuedOvpnFile>> GetAllByExternalIdAndVpnServerIdAsync(int vpnServerId, string externalId,
        CancellationToken cancellationToken, bool isRevoked = false);
    Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> 
        GetAllByExternalIdAndVpnServerIdWithTokenAsync(int vpnServerId, string externalId, 
            CancellationToken cancellationToken, bool isRevoked = false);
    
    Task<IssuedOvpnFile> AddOvpnFileAsync(AddFileRequest request, 
        CancellationToken cancellationToken);
    Task<(IssuedOvpnFile File, IssuedOvpnFileToken Token)> AddOvpnFileWithTokenAsync(AddFileRequest request, 
        CancellationToken cancellationToken);
    
    Task<IssuedOvpnFile> RevokeOvpnFileAsync(RevokeFileRequest request,
        CancellationToken cancellationToken);

    Task<DownloadFileResponse> DownloadOvpnFileAsync(DownloadFileRequest request,
        CancellationToken cancellationToken, bool isRevoked = false);
}