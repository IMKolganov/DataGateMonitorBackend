namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

/// <summary>
/// Peak / trough of a concurrent metric over sampled series buckets (not zero-filled gaps).
/// </summary>
public readonly record struct OverviewConcurrentExtreme(int Count, DateTimeOffset At);

public readonly record struct OverviewConcurrentExtremes(
    OverviewConcurrentExtreme? Peak,
    OverviewConcurrentExtreme? Low);

public static class OverviewSeriesExtremes
{
    /// <summary>
    /// Computes peak and low from buckets that actually had samples.
    /// Empty input → both null. Ties keep the earliest bucket timestamp.
    /// </summary>
    public static OverviewConcurrentExtremes Compute(
        IEnumerable<(DateTimeOffset Ts, int Value)> points)
    {
        OverviewConcurrentExtreme? peak = null;
        OverviewConcurrentExtreme? low = null;

        foreach (var (ts, value) in points)
        {
            if (peak is null || value > peak.Value.Count)
                peak = new OverviewConcurrentExtreme(value, ts);
            if (low is null || value < low.Value.Count)
                low = new OverviewConcurrentExtreme(value, ts);
        }

        return new OverviewConcurrentExtremes(peak, low);
    }
}
