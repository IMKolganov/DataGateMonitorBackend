namespace DataGateMonitor.Configurations;

public sealed class WindowsCrashIngestOptions
{
    public const string SectionName = "WindowsCrashIngest";
    public const int DefaultMaxPayloadBytes = 1024 * 1024;
    public const int DefaultRateLimitMaxRequests = 20;
    public const int DefaultRateLimitWindowSeconds = 60;
    public const int DefaultRecentMaxLimit = 200;

    public int MaxPayloadBytes { get; set; } = DefaultMaxPayloadBytes;

    public int RateLimitMaxRequests { get; set; } = DefaultRateLimitMaxRequests;

    public int RateLimitWindowSeconds { get; set; } = DefaultRateLimitWindowSeconds;

    public bool RequireHttps { get; set; } = true;

    public int RecentMaxLimit { get; set; } = DefaultRecentMaxLimit;

    public string? AuthToken { get; set; }

    public void ApplyDefaults()
    {
        if (MaxPayloadBytes <= 0)
            MaxPayloadBytes = DefaultMaxPayloadBytes;

        if (RateLimitMaxRequests <= 0)
            RateLimitMaxRequests = DefaultRateLimitMaxRequests;

        if (RateLimitWindowSeconds <= 0)
            RateLimitWindowSeconds = DefaultRateLimitWindowSeconds;

        if (RecentMaxLimit <= 0)
            RecentMaxLimit = DefaultRecentMaxLimit;
    }
}
