using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Requests;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

public interface ICertApiClient
{
    Task<List<ServerCertificate>> GetAllCertificatesAsync(int serverId, CancellationToken cancellationToken);
    Task<ServerCertificate> BuildCertificateAsync(int serverId, string commonName, CancellationToken cancellationToken);
    Task<ServerCertificate> RevokeCertificateAsync(RevokeCertificateRequest request, CancellationToken cancellationToken);
}