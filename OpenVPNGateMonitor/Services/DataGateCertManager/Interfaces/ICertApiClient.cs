using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface ICertApiClient
{
    Task<List<CertificateCaInfo>> GetAllCertificatesAsync(int serverId, CancellationToken cancellationToken);
    Task<CertificateBuildResult> BuildCertificateAsync(int serverId, string commonName, CancellationToken cancellationToken);
    Task<CertificateRevokeResult> RevokeCertificateAsync(int serverId, string commonName, CancellationToken cancellationToken);
    Task<string> GetPemContentAsync(int serverId, string filePath, CancellationToken cancellationToken);
}