namespace DataGateMonitor.SharedModels.Enums;

public enum OverviewGrouping
{
    Auto,
    Hours,
    Days,
    Months,
    Years,
    /// <summary>10-minute buckets (finer than hourly; used by Auto for ranges up to 24h).</summary>
    TenMinutes,
}
