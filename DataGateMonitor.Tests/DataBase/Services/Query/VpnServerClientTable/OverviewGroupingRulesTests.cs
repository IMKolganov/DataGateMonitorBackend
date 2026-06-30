using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class OverviewGroupingRulesTests
{
    [Fact]
    public void ResolveAuto_Within24Hours_UsesTenMinuteBuckets()
    {
        var from = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
        var to = from.AddHours(24);

        var resolved = OverviewGroupingRules.ResolveAuto(from, to, OverviewGrouping.Auto);

        Assert.Equal(OverviewGrouping.TenMinutes, resolved);
    }

    [Fact]
    public void ResolveAuto_Between24And48Hours_UsesHours()
    {
        var from = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
        var to = from.AddHours(30);

        var resolved = OverviewGroupingRules.ResolveAuto(from, to, OverviewGrouping.Auto);

        Assert.Equal(OverviewGrouping.Hours, resolved);
    }

    [Fact]
    public void ResolveAuto_ExplicitTenMinutes_IsPreserved()
    {
        var from = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(7);

        var resolved = OverviewGroupingRules.ResolveAuto(from, to, OverviewGrouping.TenMinutes);

        Assert.Equal(OverviewGrouping.TenMinutes, resolved);
    }
}
