using StackExchange.Redis;

namespace DataGateMonitor.Services.Cache;

public sealed class RedisConnectedClientsCounterStore(
    IConfiguration configuration,
    ILogger<RedisConnectedClientsCounterStore> logger) : IConnectedClientsCounterStore
{
    private const string KeyPrefix = "vpn:connected-clients:";
    private static readonly TimeSpan CounterTtl = TimeSpan.FromHours(2);
    private readonly string? _connectionString =
        configuration.GetConnectionString("Redis")
        ?? configuration["Redis:ConnectionString"]
        ?? configuration["REDIS_CONNECTION_STRING"];
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private IConnectionMultiplexer? _multiplexer;
    private bool _connectAttempted;

    public async Task<Dictionary<int, int>> GetManyAsync(IEnumerable<int> vpnServerIds, CancellationToken ct = default)
    {
        var ids = vpnServerIds.Distinct().ToArray();
        var result = new Dictionary<int, int>(ids.Length);
        if (ids.Length == 0) return result;

        var db = await GetDatabaseOrNullAsync(ct);
        if (db is null) return result;

        try
        {
            var keys = ids.Select(id => (RedisKey)$"{KeyPrefix}{id}").ToArray();
            var values = await db.StringGetAsync(keys);
            for (var i = 0; i < ids.Length; i += 1)
            {
                if (!values[i].HasValue) continue;
                if (!int.TryParse(values[i].ToString(), out var parsed)) continue;
                result[ids[i]] = Math.Max(0, parsed);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Redis read failed for connected-clients counters");
        }

        return result;
    }

    public async Task SetAsync(int vpnServerId, int connectedClientsCount, CancellationToken ct = default)
    {
        if (vpnServerId <= 0) return;

        var db = await GetDatabaseOrNullAsync(ct);
        if (db is null) return;

        try
        {
            var key = (RedisKey)$"{KeyPrefix}{vpnServerId}";
            await db.StringSetAsync(key, Math.Max(0, connectedClientsCount), CounterTtl);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Redis write failed for VpnServerId={VpnServerId}", vpnServerId);
        }
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
                logger.LogInformation("Redis connected for connected-clients counters.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis unavailable; connected-clients counters will fall back to DB.");
                _multiplexer = null;
            }

            return _multiplexer?.GetDatabase();
        }
        finally
        {
            _connectLock.Release();
        }
    }
}
