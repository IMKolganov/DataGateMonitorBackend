namespace DataGateMonitor.Services.Cache;

public interface IApiMemoryCacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);

    Task<T> GetOrCreateByStampAsync<T>(
        string key,
        string? stamp,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);

    void Set<T>(string key, T value, TimeSpan ttl, string? stamp = null);

    void Remove(string key);
}
