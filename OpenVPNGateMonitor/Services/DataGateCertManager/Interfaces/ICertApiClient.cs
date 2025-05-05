using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface ICertApiClient
{
    Task<List<CertificateCaInfo>> GetAllCertificatesAsync(CancellationToken cancellationToken);
    Task<CertificateBuildResult> BuildCertificateAsync(string commonName, CancellationToken cancellationToken);
    Task<CertificateRevokeResult> RevokeCertificateAsync(string commonName, CancellationToken cancellationToken);
    Task<string> GetPemContentAsync(string filePath, CancellationToken cancellationToken);
}