using DataGateMonitor.Services.Api.Auth.Login;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace DataGateMonitor.Tests.Services.Api.Auth.Login;

public class AdminIdleSessionTrackerTests
{
    [Fact]
    public void IsExpired_returns_true_when_never_touched()
    {
        var tracker = CreateTracker(new Dictionary<string, string?> { ["Jwt:AdminIdleTimeoutMinutes"] = "15" });

        Assert.True(tracker.IsExpired(42));
    }

    [Fact]
    public void Touch_resets_idle_timer()
    {
        var tracker = CreateTracker(new Dictionary<string, string?> { ["Jwt:AdminIdleTimeoutMinutes"] = "15" });

        tracker.Touch(42);
        Assert.False(tracker.IsExpired(42));
    }

    [Fact]
    public void ResolveIdleTimeout_uses_config_value()
    {
        var tracker = CreateTracker(new Dictionary<string, string?> { ["Jwt:AdminIdleTimeoutMinutes"] = "20" });

        Assert.Equal(TimeSpan.FromMinutes(20), tracker.IdleTimeout);
    }

    [Fact]
    public void ResolveIdleTimeout_defaults_to_15_minutes()
    {
        var tracker = CreateTracker(new Dictionary<string, string?>());

        Assert.Equal(TimeSpan.FromMinutes(15), tracker.IdleTimeout);
    }

    private static AdminIdleSessionTracker CreateTracker(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new AdminIdleSessionTracker(configuration, new MemoryCache(new MemoryCacheOptions()));
    }
}
