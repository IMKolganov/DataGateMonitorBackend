using System.Collections.Concurrent;

namespace DataGateMonitor.Services.CertExpiry;

/// <summary>In-memory deduplication so hourly polls do not spam admins for the same profile/expiry.</summary>
public sealed class CertExpiryNotificationTracker
{
    private const string ServerFailureCommonName = "_server_check_";
    private static readonly DateTimeOffset ServerFailureExpiryAnchor = DateTimeOffset.UnixEpoch;

    private readonly ConcurrentDictionary<string, byte> _sent = new();

    public bool TryMarkNotified(int vpnServerId, string commonName, DateTimeOffset expiryUtc, string alertKind)
    {
        var key = BuildKey(vpnServerId, commonName, expiryUtc, alertKind);
        return _sent.TryAdd(key, 0);
    }

    public bool TryMarkServerCheckFailureNotified(int vpnServerId) =>
        TryMarkNotified(vpnServerId, ServerFailureCommonName, ServerFailureExpiryAnchor, "server-check-failed");

    internal static string BuildKey(int vpnServerId, string commonName, DateTimeOffset expiryUtc, string alertKind) =>
        $"{vpnServerId}|{commonName}|{expiryUtc.UtcDateTime:yyyyMMddHHmmss}|{alertKind}";
}
