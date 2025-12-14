using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

public interface IOvpnFileApiService
{
    Task<IssuedOvpnFile> GetByToken(string token, CancellationToken cancellationToken
        , bool isRevoked = false);
    Task<List<IssuedOvpnFile>> GetAllByVpnServerId(int vpnServerId, CancellationToken cancellationToken);
    Task<List<IssuedOvpnFile>> GetAllByExternalId(string externalId, 
        CancellationToken cancellationToken);

    Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> GetAllByVpnServerIdWithToken(int vpnServerId,
        CancellationToken cancellationToken, bool isRevoked = false);

    Task<List<IssuedOvpnFile>> GetAllByExternalIdAndVpnServerId(int vpnServerId, string externalId,
        CancellationToken cancellationToken, bool isRevoked = false);
    Task<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>> 
        GetAllByExternalIdAndVpnServerIdWithToken(int vpnServerId, string externalId, 
            CancellationToken cancellationToken, bool isRevoked = false);
    
    Task<IssuedOvpnFile> AddOvpnFile(AddFileRequest request, 
        CancellationToken cancellationToken);
    Task<(IssuedOvpnFile File, IssuedOvpnFileToken Token)> AddOvpnFileWithToken(AddFileRequest request, 
        CancellationToken cancellationToken);
    
    Task<IssuedOvpnFile> RevokeOvpnFile(RevokeFileRequest request,
        CancellationToken cancellationToken);

    Task<DownloadFileResponse> DownloadOvpnFile(DownloadFileRequest request,
        CancellationToken cancellationToken, bool isRevoked = false);
    
    Task<DownloadFileResponse> DownloadOvpnFileByCn(DownloadFileRequest request,
        CancellationToken cancellationToken, bool isRevoked = false);
}