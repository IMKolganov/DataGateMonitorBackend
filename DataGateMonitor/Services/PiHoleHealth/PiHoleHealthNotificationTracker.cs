using System.Collections.Concurrent;

namespace DataGateMonitor.Services.PiHoleHealth;

public sealed class PiHoleHealthNotificationTracker
{
    private readonly ConcurrentDictionary<string, byte> _unhealthySent = new();
    private readonly ConcurrentDictionary<int, byte> _recoveredSent = new();

    public bool TryMarkUnhealthyNotified(int vpnServerId, string health, string healthMessage) =>
        _unhealthySent.TryAdd(BuildUnhealthyKey(vpnServerId, health, healthMessage), 0);

    public bool TryMarkRecoveredNotified(int vpnServerId) =>
        _recoveredSent.TryAdd(vpnServerId, 0);

    public bool HasUnhealthyNotification(int vpnServerId) =>
        _unhealthySent.Keys.Any(k => k.StartsWith($"{vpnServerId}|", StringComparison.Ordinal));

    public void ClearUnhealthy(int vpnServerId)
    {
        foreach (var key in _unhealthySent.Keys.Where(k => k.StartsWith($"{vpnServerId}|", StringComparison.Ordinal)))
            _unhealthySent.TryRemove(key, out _);

        _recoveredSent.TryRemove(vpnServerId, out _);
    }

    internal static string BuildUnhealthyKey(int vpnServerId, string health, string healthMessage) =>
        $"{vpnServerId}|{health}|{healthMessage}";
}
