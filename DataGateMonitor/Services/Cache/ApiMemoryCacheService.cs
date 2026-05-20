using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace DataGateMonitor.Services.Cache;

public class ApiMemoryCacheService(IMemoryCache cache) : IApiMemoryCacheService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl,
        CancellationToken ct = default)
        => GetOrCreateInternalAsync(key, null, factory, ttl, requireStampMatch: false, ct);

    public Task<T> GetOrCreateByStampAsync<T>(string key, string? stamp, Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl, CancellationToken ct = default)
        => GetOrCreateInternalAsync(key, stamp, factory, ttl, requireStampMatch: true, ct);

    public void Set<T>(string key, T value, TimeSpan ttl, string? stamp = null)
        => cache.Set(key, new CacheEnvelope<T>(value, stamp), ttl);

    public void Remove(string key)
        => cache.Remove(key);

    private async Task<T> GetOrCreateInternalAsync<T>(
        string key,
        string? stamp,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        bool requireStampMatch,
        CancellationToken ct)
    {
        if (TryGetValid(key, stamp, requireStampMatch, out T? cached))
            return cached!;

        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (TryGetValid(key, stamp, requireStampMatch, out cached))
                return cached!;

            var fresh = await factory(ct);
            Set(key, fresh, ttl, stamp);
            return fresh;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetValid<T>(string key, string? stamp, bool requireStampMatch, out T? value)
    {
        value = default;
        if (!cache.TryGetValue<CacheEnvelope<T>>(key, out var cached) || cached is null)
            return false;

        if (requireStampMatch && !string.Equals(cached.Stamp, stamp, StringComparison.Ordinal))
            return false;

        value = cached.Value;
        return true;
    }

    private sealed record CacheEnvelope<T>(T Value, string? Stamp);
}
