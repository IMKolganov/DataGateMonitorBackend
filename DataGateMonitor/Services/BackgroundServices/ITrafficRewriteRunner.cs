namespace DataGateMonitor.Services.BackgroundServices;

public interface ITrafficRewriteRunner
{
    /// <summary>
    /// Acquires the advisory lock, then processes the oldest Pending/Running job by Id
    /// (up to <see cref="TrafficRewriteOptions.MaxBatchesPerJobPass"/> batches).
    /// </summary>
    Task ProcessPendingAsync(CancellationToken ct);
}
