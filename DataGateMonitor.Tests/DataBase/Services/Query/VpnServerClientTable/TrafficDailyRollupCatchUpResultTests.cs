using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class TrafficDailyRollupCatchUpResultTests
{
    [Fact]
    public void Empty_HasNoWorkAndNullRange()
    {
        var empty = TrafficDailyRollupCatchUpResult.Empty;

        Assert.False(empty.HasWork);
        Assert.Null(empty.FirstDay);
        Assert.Null(empty.LastDay);
        Assert.Empty(empty.ProcessedDays);
        Assert.Equal(0, empty.SessionDayRowsUpserted);
    }

    [Fact]
    public void Result_ExposesFirstAndLastProcessedDay()
    {
        var result = new TrafficDailyRollupCatchUpResult(
            [new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 3)],
            7);

        Assert.True(result.HasWork);
        Assert.Equal(new DateOnly(2026, 5, 1), result.FirstDay);
        Assert.Equal(new DateOnly(2026, 5, 3), result.LastDay);
        Assert.Equal(7, result.SessionDayRowsUpserted);
    }
}
