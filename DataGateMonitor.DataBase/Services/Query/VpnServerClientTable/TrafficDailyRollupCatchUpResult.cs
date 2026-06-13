namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public sealed record TrafficDailyRollupCatchUpResult(
    IReadOnlyList<DateOnly> ProcessedDays,
    int SessionDayRowsUpserted)
{
    public static TrafficDailyRollupCatchUpResult Empty { get; } = new([], 0);

    public bool HasWork => ProcessedDays.Count > 0;

    public DateOnly? FirstDay => ProcessedDays.Count > 0 ? ProcessedDays[0] : null;

    public DateOnly? LastDay => ProcessedDays.Count > 0 ? ProcessedDays[^1] : null;
}
