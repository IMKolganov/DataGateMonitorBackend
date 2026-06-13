using NpgsqlTypes;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class OverviewTrafficDailyPostgresSqlTests
{
    [Fact]
    public void BuildFilterParams_TypesNullableParametersExplicitly()
    {
        var parameters = OverviewTrafficDailyPostgresSql.BuildFilterParams(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31),
            null,
            null);

        var byName = parameters.ToDictionary(p => p.ParameterName!);

        Assert.Equal(NpgsqlDbType.Date, byName["fromDay"].NpgsqlDbType);
        Assert.Equal(NpgsqlDbType.Date, byName["toDay"].NpgsqlDbType);
        Assert.Equal(NpgsqlDbType.Integer, byName["vpnServerId"].NpgsqlDbType);
        Assert.Equal(NpgsqlDbType.Text, byName["externalId"].NpgsqlDbType);
        Assert.Equal(DBNull.Value, byName["vpnServerId"].Value);
        Assert.Equal(DBNull.Value, byName["externalId"].Value);
    }

    [Theory]
    [InlineData(OverviewGrouping.Days, "day")]
    [InlineData(OverviewGrouping.Months, "month")]
    [InlineData(OverviewGrouping.Years, "year")]
    [InlineData(OverviewGrouping.Hours, "hour")]
    public void ResolveTruncUnit_MapsGrouping(OverviewGrouping grouping, string expected)
    {
        Assert.Equal(expected, OverviewTrafficDailyPostgresSql.ResolveTruncUnit(grouping));
    }

    [Fact]
    public void BuildFilteredDailyCte_ReferencesDailyTableAndDayRange()
    {
        var cte = OverviewTrafficDailyPostgresSql.BuildFilteredDailyCte(@"""x"".""Daily""");

        Assert.Contains(@"""x"".""Daily""", cte, StringComparison.Ordinal);
        Assert.Contains("@fromDay", cte, StringComparison.Ordinal);
        Assert.Contains("@toDay", cte, StringComparison.Ordinal);
        Assert.Contains("@vpnServerId", cte, StringComparison.Ordinal);
        Assert.Contains("@externalId", cte, StringComparison.Ordinal);
    }

    [Fact]
    public void BucketStartSql_ForDayGrouping_CastsDayColumnToTimestamp()
    {
        var sql = OverviewTrafficDailyPostgresSql.BucketStartSql("day");

        Assert.Contains("::timestamp AT TIME ZONE 'UTC'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("date_trunc('day'", sql, StringComparison.Ordinal);
    }
}
