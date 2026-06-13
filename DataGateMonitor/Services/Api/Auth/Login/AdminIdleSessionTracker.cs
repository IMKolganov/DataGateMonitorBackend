using Microsoft.Extensions.Caching.Memory;

namespace DataGateMonitor.Services.Api.Auth.Login;

public sealed class AdminIdleSessionTracker(IConfiguration configuration, IMemoryCache memoryCache)
    : IAdminIdleSessionTracker
{
    private const string AdminRole = "Admin";

    public TimeSpan IdleTimeout { get; } = ResolveIdleTimeout(configuration);

    public void Touch(int userId)
    {
        if (userId <= 0) return;

        var cacheKey = CacheKey(userId);
        memoryCache.Set(cacheKey, DateTimeOffset.UtcNow, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = IdleTimeout.Add(IdleTimeout),
        });
    }

    public bool IsExpired(int userId)
    {
        if (userId <= 0) return true;

        if (!memoryCache.TryGetValue(CacheKey(userId), out DateTimeOffset lastActivity))
            return true;

        return DateTimeOffset.UtcNow - lastActivity >= IdleTimeout;
    }

    public void Clear(int userId)
    {
        if (userId <= 0) return;
        memoryCache.Remove(CacheKey(userId));
    }

    public static bool IsAdminRole(string? role) =>
        string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase);

    private static TimeSpan ResolveIdleTimeout(IConfiguration configuration)
    {
        var minutes = configuration.GetValue<int?>("Jwt:AdminIdleTimeoutMinutes") ?? 15;
        if (minutes <= 0)
            minutes = 15;

        return TimeSpan.FromMinutes(minutes);
    }

    private static string CacheKey(int userId) => $"admin-session-idle:{userId}";
}
