namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>Tuning for <see cref="TrafficRewriteRunner"/> (tests lower MaxBatchesPerJobPass to simulate kill).</summary>
public sealed class TrafficRewriteOptions
{
    /// <summary>MeasuredAt window size per UPDATE batch on stock PostgreSQL.</summary>
    public TimeSpan BatchWindow { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Max MeasuredAt batches processed for one job in a single runner pass.
    /// Leave as <see cref="int.MaxValue"/> in production; set to 1 in kill/restart tests.
    /// </summary>
    public int MaxBatchesPerJobPass { get; set; } = int.MaxValue;

    /// <summary>When true (PostgreSQL only), run VACUUM ANALYZE on traffics after each batch — outside any EF transaction.</summary>
    public bool VacuumAfterBatch { get; set; } = true;
}
