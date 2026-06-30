using DataGateMonitor.Services.CertExpiry;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryNotificationTrackerTests
{
    [Fact]
    public void TryMarkNotified_ReturnsFalse_OnDuplicateKey()
    {
        var tracker = new CertExpiryNotificationTracker();
        var expiry = new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

        Assert.True(tracker.TryMarkNotified(1, "client-a", expiry, "expiring-soon"));
        Assert.False(tracker.TryMarkNotified(1, "client-a", expiry, "expiring-soon"));
    }

    [Fact]
    public void TryMarkNotified_AllowsDifferentAlertKinds()
    {
        var tracker = new CertExpiryNotificationTracker();
        var expiry = new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

        Assert.True(tracker.TryMarkNotified(1, "client-a", expiry, "expiring-soon"));
        Assert.True(tracker.TryMarkNotified(1, "client-a", expiry, "expired"));
    }

    [Fact]
    public void TryMarkServerCheckFailureNotified_DeduplicatesPerServer()
    {
        var tracker = new CertExpiryNotificationTracker();

        Assert.True(tracker.TryMarkServerCheckFailureNotified(7));
        Assert.False(tracker.TryMarkServerCheckFailureNotified(7));
        Assert.True(tracker.TryMarkServerCheckFailureNotified(8));
    }

    [Fact]
    public void BuildKey_IsStableForUtcAndLocalSameInstant()
    {
        var utc = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var local = utc.ToOffset(TimeSpan.FromHours(3));

        var utcKey = CertExpiryNotificationTracker.BuildKey(1, "cn", utc, "expired");
        var localKey = CertExpiryNotificationTracker.BuildKey(1, "cn", local, "expired");

        Assert.Equal(utcKey, localKey);
    }
}
