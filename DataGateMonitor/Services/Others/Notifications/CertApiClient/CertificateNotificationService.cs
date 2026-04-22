using DataGateMonitor.Services.Others.Models;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Requests;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications.CertApiClient;

public class CertificateNotificationService(INotificationService notifications)
    : ICertificateNotificationService
{
    private static readonly string[] ReadChannels = ["web"];
    private static readonly string[] ChangeChannels = ["web", "telegram"];

    public Task NotifyReadAllAsync(int vpnServerId, int count, CancellationToken ct)
        => Notify(
            ApplicationNotificationKind.CertApiReadAll,
            type: "cert.read.all",
            title: "All certificates requested",
            message: $"ServerId={vpnServerId}; Count={count}",
            serverId: vpnServerId,
            severity: NotificationSeverity.Info,
            channels: ReadChannels,
            ct: ct
        );

    public Task NotifyBuiltAsync(int vpnServerId, ServerCertificate certificate, CancellationToken ct)
        => Notify(
            ApplicationNotificationKind.CertApiCertificateCreated,
            type: "cert.built",
            title: "Certificate created",
            message:
            $"CommonName={Short(certificate.CommonName)}; Serial={Short(certificate.SerialNumber)}; ExpiresAt={certificate.ExpiryDate:O}",
            serverId: vpnServerId,
            severity: NotificationSeverity.Info,
            channels: ChangeChannels,
            ct: ct
        );

    public Task NotifyRevokedAsync(int vpnServerId, RevokeCertificateRequest request,
        ServerCertificate? certificate, CancellationToken ct)
        => Notify(
            ApplicationNotificationKind.CertApiCertificateRevoked,
            type: "cert.revoked",
            title: "Certificate revoked",
            message: $"CommonName={Short(request.CommonName)}; Serial={Short(certificate?.SerialNumber)}",
            serverId: vpnServerId,
            severity: NotificationSeverity.Warning,
            channels: ChangeChannels,
            ct: ct
        );

    // ---- helper ----
    private Task Notify(ApplicationNotificationKind preferenceKind, string type, string title, string message, int serverId,
        NotificationSeverity severity, string[] channels, CancellationToken ct, int? actorUserId = null)
        => notifications.NotifyAdmins(new NotificationRequest
        {
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            Source = "cert-api",
            ServerId = serverId,
            ActorUserId = actorUserId,
            PreferenceKind = preferenceKind
        }, channels, ct);

    private static string Short(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length > 12 ? $"{value[..6]}…{value[^4..]}" : value;
    }
}