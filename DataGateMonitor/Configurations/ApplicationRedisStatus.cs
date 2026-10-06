using DataGateMonitor.Services.Cache;
using StackExchange.Redis;

namespace DataGateMonitor.Configurations;

public readonly record struct RedisStatusSnapshot(bool IsConfigured, string Line, string Tone);

/// <summary>
/// Resolves Redis status for the public root page. Hidden when Redis is not configured.
/// </summary>
public static class ApplicationRedisStatus
{
    public static bool IsConfigured(IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"]
            ?? configuration["REDIS_CONNECTION_STRING"];
        return !string.IsNullOrWhiteSpace(connectionString);
    }

    public static async Task<RedisStatusSnapshot> ResolveAsync(
        IConfiguration configuration,
        IRedisDatabaseProvider redisDatabaseProvider,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured(configuration))
            return new RedisStatusSnapshot(false, "", "");

        try
        {
            var database = await redisDatabaseProvider.GetDatabaseAsync(cancellationToken);
            if (database is null)
            {
                return new RedisStatusSnapshot(
                    true,
                    "Configured but unavailable (cache falls back where needed).",
                    "error");
            }

            var latency = await database.PingAsync();
            var ms = latency.TotalMilliseconds;
            var line = ms < 1
                ? "Connected (ping <1 ms)."
                : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Connected (ping {ms:0} ms).");
            return new RedisStatusSnapshot(true, line, "ok");
        }
        catch (RedisException ex)
        {
            return new RedisStatusSnapshot(true, $"Unavailable — {ex.Message}", "error");
        }
        catch (Exception ex)
        {
            return new RedisStatusSnapshot(true, $"Unavailable — {ex.Message}", "error");
        }
    }
}
