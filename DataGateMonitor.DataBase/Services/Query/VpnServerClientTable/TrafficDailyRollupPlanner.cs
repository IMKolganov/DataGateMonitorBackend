namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

/// <summary>
/// Pure logic for finding UTC calendar days that need a daily traffic rollup.
/// </summary>
public static class TrafficDailyRollupPlanner
{
    /// <summary>
    /// Days with raw poll samples that do not yet have any daily rollup row, capped at <paramref name="throughDayInclusive"/>.
    /// </summary>
    public static IReadOnlyList<DateOnly> FindMissingDays(
        IEnumerable<DateOnly> rawTrafficDays,
        IEnumerable<DateOnly> rolledUpDays,
        DateOnly throughDayInclusive)
    {
        var rolled = rolledUpDays as IReadOnlySet<DateOnly> ?? rolledUpDays.ToHashSet();
        return rawTrafficDays
            .Where(d => d <= throughDayInclusive)
            .Where(d => !rolled.Contains(d))
            .Distinct()
            .OrderBy(d => d)
            .ToList();
    }

    /// <summary>Yesterday in UTC — last fully completed calendar day.</summary>
    public static DateOnly YesterdayUtc(DateTime utcNow)
        => DateOnly.FromDateTime(utcNow.AddDays(-1));

    public static IEnumerable<DateOnly> EnumerateDaysInclusive(DateOnly from, DateOnly toInclusive)
    {
        if (toInclusive < from)
            yield break;

        for (var day = from; day <= toInclusive; day = day.AddDays(1))
            yield return day;
    }
}
