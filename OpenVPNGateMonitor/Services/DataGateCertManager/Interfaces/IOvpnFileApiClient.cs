using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Requests;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface IOvpnFileApiClient
{
    Task<IssuedOvpnFile> AddOvpnFileAsync(int vpnServerId, AddOvpnFileRequest request, CancellationToken cancellationToken);
    Task<bool> RevokeOvpnFileAsync(int vpnServerId, RevokeOvpnFileRequest request, CancellationToken cancellationToken);
    Task<string> DownloadOvpnFileAsync(int vpnServerId, DownloadOvpnFileRequest request, CancellationToken cancellationToken);
}