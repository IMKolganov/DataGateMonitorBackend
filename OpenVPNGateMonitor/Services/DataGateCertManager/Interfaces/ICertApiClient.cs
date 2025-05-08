using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.Cert.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Requests;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

public interface ICertApiClient
{
    Task<List<ServerCertificate>> GetAllCertificatesAsync(int serverId, CancellationToken cancellationToken);
    Task<ServerCertificate> BuildCertificateAsync(int serverId, string commonName, CancellationToken cancellationToken);
    Task<CertificateRevokeResult> RevokeCertificateAsync(RevokeCertificateRequest request, CancellationToken cancellationToken);
}