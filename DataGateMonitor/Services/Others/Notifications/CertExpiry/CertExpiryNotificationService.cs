using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Requests;

namespace DataGateMonitor.Services.Others.Notifications.CertExpiry;

public sealed class CertExpiryNotificationService(INotificationService notifications) : ICertExpiryNotificationService
{
    private static readonly string[] Channels = ["web", "telegram"];
    private const string Source = "ovpn-cert-expiry";

    public Task NotifyExpiringSoonAsync(
        IssuedOvpnFile issuedFile,
        string serverName,
        DateTimeOffset expiryUtc,
        int daysLeft,
        string? serialNumber,
        CancellationToken ct)
        => Notify(
            ApplicationNotificationKind.OvpnCertExpiryWarning,
            NotificationTypes.OvpnCertExpiryWarning,
            "OpenVPN client certificate expiring soon",
            BuildMessage(issuedFile, serverName, expiryUtc, serialNumber,
                $"Expires in {daysLeft} day(s). Issue a new profile or revoke and re-issue."),
            issuedFile.VpnServerId,
            NotificationSeverity.Warning,
            ct);

    public Task NotifyExpiredAsync(
        IssuedOvpnFile issuedFile,
        string serverName,
        DateTimeOffset expiryUtc,
        string? serialNumber,
        CancellationToken ct)
        => Notify(
            ApplicationNotificationKind.OvpnCertExpired,
            NotificationTypes.OvpnCertExpired,
            "OpenVPN client certificate expired",
            BuildMessage(issuedFile, serverName, expiryUtc, serialNumber,
                "Clients can no longer connect with this profile. Revoke and issue a replacement."),
            issuedFile.VpnServerId,
            NotificationSeverity.Error,
            ct);

    public Task NotifyCertificateMissingAsync(
        IssuedOvpnFile issuedFile,
        string serverName,
        CancellationToken ct)
        => Notify(
            ApplicationNotificationKind.OvpnCertExpiryWarning,
            NotificationTypes.OvpnCertExpiryWarning,
            "OpenVPN certificate missing on node",
            $"Server={serverName} (id {issuedFile.VpnServerId}); " +
            $"IssuedOvpnFileId={issuedFile.Id}; CN={issuedFile.CommonName}; ExternalId={issuedFile.ExternalId}. " +
            "Active profile in DB but no matching certificate on the node PKI.",
            issuedFile.VpnServerId,
            NotificationSeverity.Warning,
            ct);

    public Task NotifyServerCheckFailedAsync(int vpnServerId, string serverName, string detail, CancellationToken ct)
        => Notify(
            ApplicationNotificationKind.OvpnCertExpiryWarning,
            NotificationTypes.OvpnCertExpiryWarning,
            "OpenVPN certificate expiry check failed",
            $"Server={serverName} (id {vpnServerId}). {detail}",
            vpnServerId,
            NotificationSeverity.Error,
            ct);

    private static string BuildMessage(
        IssuedOvpnFile issuedFile,
        string serverName,
        DateTimeOffset expiryUtc,
        string? serialNumber,
        string action)
    {
        var serial = string.IsNullOrWhiteSpace(serialNumber) ? "n/a" : serialNumber;
        return $"Server={serverName} (id {issuedFile.VpnServerId}); " +
               $"IssuedOvpnFileId={issuedFile.Id}; CN={issuedFile.CommonName}; ExternalId={issuedFile.ExternalId}; " +
               $"Serial={serial}; ExpiresAt={expiryUtc:O}. {action}";
    }

    private Task Notify(
        ApplicationNotificationKind preferenceKind,
        string type,
        string title,
        string message,
        int serverId,
        NotificationSeverity severity,
        CancellationToken ct)
        => notifications.NotifyAdmins(new NotifyAdminsRequest
        {
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            Source = Source,
            ServerId = serverId,
            PreferenceKind = preferenceKind
        }, Channels, ct);
}
