using System.Collections.Concurrent;
using System.Text.Json;
using StackExchange.Redis;

namespace DataGateMonitor.Services.StatusStreamLogs;

public sealed class StatusStreamLogStore(
    IConfiguration configuration,
    ILogger<StatusStreamLogStore> logger) : IStatusStreamLogStore
{
    private const string RedisListKey = "vpn:status-stream:logs";
    private const string RedisSource = "redis";
    private const string MemorySource = "memory";
    private const int DefaultMaxEntries = 300;
    private const int MaxAllowedEntries = 2000;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentQueue<StatusStreamLogEntry> _memoryQueue = new();
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly string? _connectionString =
        configuration.GetConnectionString("Redis")
        ?? configuration["Redis:ConnectionString"]
        ?? configuration["REDIS_CONNECTION_STRING"];
    private readonly int _maxEntries = ResolveMaxEntries(configuration);
    private IConnectionMultiplexer? _multiplexer;
    private bool _connectAttempted;

    public async Task AppendAsync(StatusStreamLogEntry entry, CancellationToken ct = default)
    {
        var normalized = new StatusStreamLogEntry
        {
            TimestampUtc = entry.TimestampUtc == default ? DateTimeOffset.UtcNow : entry.TimestampUtc,
            PayloadJson = entry.PayloadJson,
            Source = entry.Source
        };

        EnqueueMemory(normalized);

        var db = await GetDatabaseOrNullAsync(ct);
        if (db is null)
            return;

        try
        {
            var redisEntry = new StatusStreamLogEntry
            {
                TimestampUtc = normalized.TimestampUtc,
                PayloadJson = normalized.PayloadJson,
                Source = RedisSource
            };
            var redisValue = JsonSerializer.Serialize(redisEntry, _jsonOptions);
            await db.ListLeftPushAsync(RedisListKey, redisValue);
            await db.ListTrimAsync(RedisListKey, 0, _maxEntries - 1);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Redis write failed for status-stream logs");
        }
    }

    public async Task<IReadOnlyList<StatusStreamLogEntry>> GetLatestAsync(int limit, CancellationToken ct = default)
    {
        var normalizedLimit = NormalizeLimit(limit);
        var db = await GetDatabaseOrNullAsync(ct);
        if (db is not null)
        {
            try
            {
                var values = await db.ListRangeAsync(RedisListKey, 0, normalizedLimit - 1);
                if (values.Length > 0)
                {
                    var list = new List<StatusStreamLogEntry>(values.Length);
                    foreach (var value in values)
                    {
                        if (!value.HasValue)
                            continue;

                        var raw = value.ToString();
                        if (string.IsNullOrWhiteSpace(raw))
                            continue;

                        var parsed = JsonSerializer.Deserialize<StatusStreamLogEntry>(raw, _jsonOptions);
                        if (parsed is null)
                            continue;

                        list.Add(new StatusStreamLogEntry
                        {
                            TimestampUtc = parsed.TimestampUtc,
                            PayloadJson = parsed.PayloadJson,
                            Source = RedisSource
                        });
                    }

                    return list;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Redis read failed for status-stream logs");
            }
        }

        return GetMemorySnapshot(normalizedLimit);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        _memoryQueue.Clear();

        var db = await GetDatabaseOrNullAsync(ct);
        if (db is null)
            return;

        try
        {
            await db.KeyDeleteAsync(RedisListKey);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Redis delete failed for status-stream logs");
        }
    }

    private void EnqueueMemory(StatusStreamLogEntry entry)
    {
        _memoryQueue.Enqueue(new StatusStreamLogEntry
        {
            TimestampUtc = entry.TimestampUtc,
            PayloadJson = entry.PayloadJson,
            Source = MemorySource
        });

        while (_memoryQueue.Count > _maxEntries && _memoryQueue.TryDequeue(out _))
        {
            // drop oldest entries
        }
    }

    private IReadOnlyList<StatusStreamLogEntry> GetMemorySnapshot(int limit)
    {
        var array = _memoryQueue.ToArray();
        if (array.Length == 0)
            return [];

        var take = Math.Min(limit, array.Length);
        var start = array.Length - take;
        var list = new List<StatusStreamLogEntry>(take);
        for (var i = array.Length - 1; i >= start; i--)
        {
            var item = array[i];
            list.Add(new StatusStreamLogEntry
            {
                TimestampUtc = item.TimestampUtc,
                PayloadJson = item.PayloadJson,
                Source = MemorySource
            });
        }

        return list;
    }

    private async Task<IDatabase?> GetDatabaseOrNullAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return null;

        if (_multiplexer is { IsConnected: true })
            return _multiplexer.GetDatabase();

        await _connectLock.WaitAsync(ct);
        try
        {
            if (_multiplexer is { IsConnected: true })
                return _multiplexer.GetDatabase();

            if (_connectAttempted)
                return null;

            _connectAttempted = true;
            try
            {
                _multiplexer = await ConnectionMultiplexer.ConnectAsync(_connectionString);
                logger.LogInformation("Redis connected for status-stream logs.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis unavailable; status-stream logs will remain in memory.");
                _multiplexer = null;
            }

            return _multiplexer?.GetDatabase();
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private static int ResolveMaxEntries(IConfiguration configuration)
    {
        var configured =
            configuration.GetValue<int?>("StatusStreamLogs:MaxEntries")
            ?? configuration.GetValue<int?>("STATUS_STREAM_LOGS_MAX_ENTRIES")
            ?? DefaultMaxEntries;

        if (configured <= 0)
            return DefaultMaxEntries;

        return Math.Min(configured, MaxAllowedEntries);
    }

    private int NormalizeLimit(int limit)
    {
        if (limit <= 0)
            return _maxEntries;

        return Math.Min(limit, _maxEntries);
    }
}
