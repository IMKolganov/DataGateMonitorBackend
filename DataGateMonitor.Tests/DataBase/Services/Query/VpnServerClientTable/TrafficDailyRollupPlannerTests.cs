using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class TrafficDailyRollupPlannerTests
{
    [Fact]
    public void FindMissingDays_ReturnsRawDaysWithoutRollup_ThroughInclusive()
    {
        var raw = new[] { new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2), new DateOnly(2026, 5, 4) };
        var rolled = new[] { new DateOnly(2026, 5, 1) };

        var missing = TrafficDailyRollupPlanner.FindMissingDays(raw, rolled, new DateOnly(2026, 5, 3));

        Assert.Equal([new DateOnly(2026, 5, 2)], missing);
    }

    [Fact]
    public void FindMissingDays_DetectsGapInMiddle_NotOnlyTail()
    {
        var raw = TrafficDailyRollupPlanner.EnumerateDaysInclusive(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 5));
        var rolled = new[]
        {
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 2),
            new DateOnly(2026, 5, 4),
            new DateOnly(2026, 5, 5),
        };

        var missing = TrafficDailyRollupPlanner.FindMissingDays(raw, rolled, new DateOnly(2026, 5, 5));

        Assert.Equal([new DateOnly(2026, 5, 3)], missing);
    }

    [Fact]
    public void FindMissingDays_ExcludesFutureDaysBeyondThrough()
    {
        var raw = new[] { new DateOnly(2026, 5, 30), new DateOnly(2026, 5, 31) };
        var missing = TrafficDailyRollupPlanner.FindMissingDays(raw, [], new DateOnly(2026, 5, 30));

        Assert.Equal([new DateOnly(2026, 5, 30)], missing);
    }

    [Fact]
    public void FindMissingDays_WhenFullyRolled_ReturnsEmpty()
    {
        var day = new DateOnly(2026, 5, 10);
        var missing = TrafficDailyRollupPlanner.FindMissingDays([day], [day], day);

        Assert.Empty(missing);
    }

    [Fact]
    public void YesterdayUtc_UsesUtcCalendarDay()
    {
        var utcNow = new DateTime(2026, 5, 31, 0, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 5, 30), TrafficDailyRollupPlanner.YesterdayUtc(utcNow));
    }

    [Fact]
    public void EnumerateDaysInclusive_ReturnsOrderedRange()
    {
        var days = TrafficDailyRollupPlanner
            .EnumerateDaysInclusive(new DateOnly(2026, 5, 28), new DateOnly(2026, 5, 30))
            .ToList();

        Assert.Equal(
            [new DateOnly(2026, 5, 28), new DateOnly(2026, 5, 29), new DateOnly(2026, 5, 30)],
            days);
    }

    [Fact]
    public void EnumerateDaysInclusive_WhenToBeforeFrom_YieldsNothing()
    {
        Assert.Empty(
            TrafficDailyRollupPlanner
                .EnumerateDaysInclusive(new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 9))
                .ToList());
    }
}
