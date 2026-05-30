namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public interface IOverviewTrafficDailyRollupService
{
    /// <summary>Computes and upserts daily deltas for one UTC calendar day from raw poll samples.</summary>
    Task<int> RollupDayAsync(DateOnly dayUtc, CancellationToken ct = default);

    /// <summary>Backfills [fromDay; toDay] inclusive, one day per transaction.</summary>
    Task<int> BackfillRangeAsync(DateOnly fromDayUtc, DateOnly toDayUtc, CancellationToken ct = default);

    /// <summary>First day with raw samples and last day already rolled up (if any).</summary>
    Task<(DateOnly? FirstRawDay, DateOnly? LastRolledUpDay)> GetCoverageAsync(CancellationToken ct = default);

    /// <summary>UTC days with raw samples but no daily rollup rows, through the given day inclusive.</summary>
    Task<IReadOnlyList<DateOnly>> GetMissingRollupDaysAsync(DateOnly throughDayUtc, CancellationToken ct = default);

    /// <summary>Rolls up every missing day through <paramref name="throughDayUtc"/>; stops on first failure.</summary>
    Task<TrafficDailyRollupCatchUpResult> CatchUpMissingDaysAsync(DateOnly throughDayUtc, CancellationToken ct = default);
}
