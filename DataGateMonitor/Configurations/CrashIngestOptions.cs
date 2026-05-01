namespace DataGateMonitor.Configurations;

public sealed class CrashIngestOptions
{
    public const string SectionName = "CrashIngest";

    public int MaxPayloadBytes { get; set; } = 1024 * 1024;

    public int RateLimitMaxRequests { get; set; } = 20;

    public int RateLimitWindowSeconds { get; set; } = 60;

    public bool RequireHttps { get; set; } = true;

    public int RecentMaxLimit { get; set; } = 200;

    public string? AuthToken { get; set; }
}
