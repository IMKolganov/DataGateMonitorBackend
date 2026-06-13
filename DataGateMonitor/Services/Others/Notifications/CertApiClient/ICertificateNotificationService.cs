using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Requests;

namespace DataGateMonitor.Services.Others.Notifications.CertApiClient;

public interface ICertificateNotificationService
{
    Task NotifyReadAllAsync(int vpnServerId, int count, CancellationToken ct);
    Task NotifyBuiltAsync(int vpnServerId, ServerCertificate certificate, CancellationToken ct);

    Task NotifyRevokedAsync(int vpnServerId, RevokeCertificateRequest request, ServerCertificate? certificate,
        CancellationToken ct);
}