namespace DataGateMonitor.Services.Api.MobileCrashIngest;

public interface ICrashIngestRateLimiter
{
    bool TryConsume(string key, out int retryAfterSeconds);
}
