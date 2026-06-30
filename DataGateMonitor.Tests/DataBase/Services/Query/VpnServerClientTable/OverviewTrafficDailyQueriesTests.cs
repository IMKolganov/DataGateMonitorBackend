using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class OverviewTrafficDailyQueriesTests
{
    [Fact]
    public void SplitRange_HistoryOnly_UsesDailySliceWithoutRawTail()
    {
        var from = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);

        var split = OverviewTrafficDailyQueries.SplitRange(from, to);

        Assert.Equal(new DateOnly(2026, 5, 1), split.DailyFrom);
        Assert.Equal(new DateOnly(2026, 5, 10), split.DailyToExclusive);
        Assert.Null(split.RawFromUtc);
        Assert.Null(split.RawToUtc);
    }

    [Fact]
    public void SplitRange_IncludingToday_AddsRawTailFromUtcMidnight()
    {
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var from = todayStart.AddDays(-3);
        var to = todayStart.AddHours(12);

        var split = OverviewTrafficDailyQueries.SplitRange(from, to);

        Assert.Equal(DateOnly.FromDateTime(from.UtcDateTime), split.DailyFrom);
        Assert.Equal(DateOnly.FromDateTime(todayStart.UtcDateTime), split.DailyToExclusive);
        Assert.Equal(todayStart, split.RawFromUtc);
        Assert.Equal(to, split.RawToUtc);
    }

    [Fact]
    public void SplitRange_TodayOnly_SkipsDailyAndUsesRaw()
    {
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var from = todayStart.AddHours(1);
        var to = todayStart.AddHours(5);

        var split = OverviewTrafficDailyQueries.SplitRange(from, to);

        Assert.Equal(DateOnly.FromDateTime(from.UtcDateTime), split.DailyFrom);
        Assert.Equal(split.DailyFrom, split.DailyToExclusive);
        Assert.Equal(from, split.RawFromUtc);
        Assert.Equal(to, split.RawToUtc);
    }

    [Fact]
    public void SplitRange_SwapsReversedBounds()
    {
        var a = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var b = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);

        var split = OverviewTrafficDailyQueries.SplitRange(b, a);

        Assert.Equal(new DateOnly(2026, 5, 1), split.DailyFrom);
        Assert.True(split.DailyToExclusive >= split.DailyFrom);
    }

    [Theory]
    [InlineData(OverviewGrouping.Days, true)]
    [InlineData(OverviewGrouping.Months, true)]
    [InlineData(OverviewGrouping.Years, true)]
    [InlineData(OverviewGrouping.Hours, false)]
    [InlineData(OverviewGrouping.TenMinutes, false)]
    public void SupportsDailyGrouping_MatchesGrouping(OverviewGrouping grouping, bool expected)
    {
        Assert.Equal(expected, OverviewTrafficDailyQueries.SupportsDailyGrouping(grouping));
    }

    [Fact]
    public void CombineTotals_SumsTrafficBytes()
    {
        var combined = OverviewTrafficDailyQueries.CombineTotals(
            new OverviewTrafficTotalsRow { TrafficInBytes = 100, TrafficOutBytes = 50 },
            new OverviewTrafficTotalsRow { TrafficInBytes = 20, TrafficOutBytes = 5 });

        Assert.Equal(120, combined.TrafficInBytes);
        Assert.Equal(55, combined.TrafficOutBytes);
    }

    [Fact]
    public void MergeBuckets_OrdersByBucketTimestamp()
    {
        var t1 = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero);

        var merged = OverviewTrafficDailyQueries.MergeBuckets(
            [new OverviewTrafficBucketRow { BucketTs = t2, ActiveClients = 1, TrafficInBytes = 1, TrafficOutBytes = 1 }],
            [new OverviewTrafficBucketRow { BucketTs = t1, ActiveClients = 2, TrafficInBytes = 2, TrafficOutBytes = 2 }]);

        Assert.Equal(t1, merged[0].BucketTs);
        Assert.Equal(t2, merged[1].BucketTs);
    }

    [Fact]
    public void MergeUserTrafficRows_CombinesDailyAndRawRowsByExternalId()
    {
        var daily = new OverviewUserTrafficRow
        {
            ExternalId = "user-a",
            VpnServerId = 1,
            Sessions = 2,
            TrafficInBytes = 100,
            TrafficOutBytes = 50,
            FirstSeen = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            LastSeen = new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.Zero),
        };
        var raw = new OverviewUserTrafficRow
        {
            ExternalId = "user-a",
            VpnServerId = 1,
            Sessions = 1,
            TrafficInBytes = 10,
            TrafficOutBytes = 5,
            FirstSeen = new DateTimeOffset(2026, 5, 31, 8, 0, 0, TimeSpan.Zero),
            LastSeen = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
        };
        var rawOnly = new OverviewUserTrafficRow
        {
            ExternalId = "user-b",
            Sessions = 1,
            TrafficInBytes = 500,
            TrafficOutBytes = 0,
            FirstSeen = new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            LastSeen = new DateTimeOffset(2026, 5, 31, 10, 0, 0, TimeSpan.Zero),
        };

        var merged = OverviewTrafficDailyQueries.MergeUserTrafficRows([daily], [raw, rawOnly]);

        Assert.Equal(2, merged.Count);
        Assert.Equal("user-b", merged[0].ExternalId);
        Assert.Equal("user-a", merged[1].ExternalId);
        Assert.Equal(110, merged[1].TrafficInBytes);
        Assert.Equal(55, merged[1].TrafficOutBytes);
        Assert.Equal(3, merged[1].Sessions);
        Assert.Equal(daily.FirstSeen, merged[1].FirstSeen);
        Assert.Equal(raw.LastSeen, merged[1].LastSeen);
    }
}
