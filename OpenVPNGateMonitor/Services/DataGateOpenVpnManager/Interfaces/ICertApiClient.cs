using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Requests;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

public interface ICertApiClient
{
    Task<List<ServerCertificate>> GetAllCertificatesAsync(int serverId, CancellationToken cancellationToken);
    Task<ServerCertificate> BuildCertificateAsync(int serverId, string commonName, CancellationToken cancellationToken);
    Task<ServerCertificate> RevokeCertificateAsync(RevokeCertificateRequest request, CancellationToken cancellationToken);
}