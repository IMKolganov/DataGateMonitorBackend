namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>
/// Cross-instance mutex so only one TrafficRewrite worker runs at a time
/// (pg_try_advisory_lock on PostgreSQL; always acquired in tests / SQLite).
/// </summary>
public interface ITrafficRewriteAdvisoryLock
{
    Task<bool> TryAcquireAsync(CancellationToken ct);

    Task ReleaseAsync(CancellationToken ct);
}
