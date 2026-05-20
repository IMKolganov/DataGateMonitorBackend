namespace DataGateMonitor.Services.Cache;

public sealed class StatusCacheGenerationService : IStatusCacheGenerationService
{
    private long _generation = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public string CurrentStamp => Interlocked.Read(ref _generation).ToString();

    public long Bump() => Interlocked.Increment(ref _generation);
}
