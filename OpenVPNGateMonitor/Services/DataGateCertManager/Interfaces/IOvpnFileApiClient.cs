using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface IOvpnFileApiClient
{
    Task<IssuedOvpnFile> AddOvpnFileAsync(AddOvpnFileRequest request, CancellationToken cancellationToken);
    Task<bool> RevokeOvpnFileAsync(RevokeOvpnFileRequest request, CancellationToken cancellationToken);
    Task<string> DownloadOvpnFileAsync(DownloadOvpnFileRequest request, CancellationToken cancellationToken);
}