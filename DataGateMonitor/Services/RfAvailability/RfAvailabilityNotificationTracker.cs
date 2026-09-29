using System.Collections.Concurrent;

namespace DataGateMonitor.Services.RfAvailability;

public sealed class RfAvailabilityNotificationTracker
{
    private readonly ConcurrentDictionary<string, byte> _unreachableSent = new();
    private readonly ConcurrentDictionary<string, byte> _recoveredSent = new();

    public bool TryMarkUnreachableNotified(string targetUrl, string summary) =>
        _unreachableSent.TryAdd(BuildKey(targetUrl, summary), 0);

    public bool TryMarkRecoveredNotified(string targetUrl) =>
        _recoveredSent.TryAdd(Normalize(targetUrl), 0);

    public bool HasUnreachableNotification(string targetUrl)
    {
        var prefix = Normalize(targetUrl) + "|";
        return _unreachableSent.Keys.Any(k => k.StartsWith(prefix, StringComparison.Ordinal));
    }

    public void ClearUnreachable(string targetUrl)
    {
        var prefix = Normalize(targetUrl) + "|";
        foreach (var key in _unreachableSent.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
            _unreachableSent.TryRemove(key, out _);

        _recoveredSent.TryRemove(Normalize(targetUrl), out _);
    }

    internal static string BuildKey(string targetUrl, string summary) =>
        $"{Normalize(targetUrl)}|{summary}";

    private static string Normalize(string targetUrl) =>
        (targetUrl ?? string.Empty).Trim().ToLowerInvariant();
}
