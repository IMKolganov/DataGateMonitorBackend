using DataGateMonitor.Models;
using DataGateMonitor.Services.Others.Notifications.CertExpiry;

namespace DataGateMonitor.Services.Others.Notifications.CertExpiry;

public interface ICertExpiryNotificationService
{
    Task NotifyExpiringSoonAsync(
        IssuedOvpnFile issuedFile,
        string serverName,
        DateTimeOffset expiryUtc,
        int daysLeft,
        string? serialNumber,
        CancellationToken ct);

    Task NotifyExpiredAsync(
        IssuedOvpnFile issuedFile,
        string serverName,
        DateTimeOffset expiryUtc,
        string? serialNumber,
        CancellationToken ct);

    Task NotifyCertificateMissingAsync(
        IssuedOvpnFile issuedFile,
        string serverName,
        CancellationToken ct);

    Task NotifyServerCheckFailedAsync(int vpnServerId, string serverName, string detail, CancellationToken ct);
}
