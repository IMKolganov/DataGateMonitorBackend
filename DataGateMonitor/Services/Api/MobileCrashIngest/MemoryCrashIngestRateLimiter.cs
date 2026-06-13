using System.Collections.Concurrent;
using DataGateMonitor.Configurations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DataGateMonitor.Services.Api.MobileCrashIngest;

public sealed class MemoryCrashIngestRateLimiter(
    IMemoryCache cache,
    IOptions<CrashIngestOptions> options) : ICrashIngestRateLimiter
{
    private static readonly ConcurrentDictionary<string, object> Locks = new();

    public bool TryConsume(string key, out int retryAfterSeconds)
    {
        var windowSeconds = Math.Max(1, options.Value.RateLimitWindowSeconds);
        var maxRequests = Math.Max(1, options.Value.RateLimitMaxRequests);
        retryAfterSeconds = windowSeconds;

        var lockObject = Locks.GetOrAdd(key, _ => new object());
        lock (lockObject)
        {
            var now = DateTimeOffset.UtcNow;
            if (!cache.TryGetValue(key, out RateLimitEntry? entry) || entry is null)
            {
                cache.Set(key, new RateLimitEntry(now, 1), TimeSpan.FromSeconds(windowSeconds + 1));
                return true;
            }

            var elapsed = now - entry.FirstRequestUtc;
            if (elapsed.TotalSeconds > windowSeconds)
            {
                cache.Set(key, new RateLimitEntry(now, 1), TimeSpan.FromSeconds(windowSeconds + 1));
                return true;
            }

            if (entry.Count >= maxRequests)
            {
                var secondsLeft = windowSeconds - (int)elapsed.TotalSeconds;
                retryAfterSeconds = Math.Max(1, secondsLeft);
                return false;
            }

            cache.Set(key, new RateLimitEntry(entry.FirstRequestUtc, entry.Count + 1), TimeSpan.FromSeconds(windowSeconds + 1));
            return true;
        }
    }

    private sealed record RateLimitEntry(DateTimeOffset FirstRequestUtc, int Count);
}
