using Npgsql;
using NpgsqlTypes;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

/// <summary>
/// Guards against PostgreSQL 42P08 when optional overview filters are NULL (all servers / all users).
/// Production SQL path is not exercised by in-memory overview query tests.
/// </summary>
public class OverviewTrafficPostgresSqlTests
{
    [Fact]
    public void BuildFilterParams_AllServersNullFilters_TypesNullableParametersExplicitly()
    {
        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(30);

        var parameters = OverviewTrafficPostgresSql.BuildFilterParams(from, to, TimeSpan.FromHours(3), null, null);

        var byName = parameters.ToDictionary(p => p.ParameterName!);

        Assert.Equal(NpgsqlDbType.TimestampTz, byName["from"].NpgsqlDbType);
        Assert.Equal(NpgsqlDbType.TimestampTz, byName["to"].NpgsqlDbType);
        Assert.Equal(NpgsqlDbType.Interval, byName["offset"].NpgsqlDbType);
        Assert.Equal(NpgsqlDbType.Integer, byName["vpnServerId"].NpgsqlDbType);
        Assert.Equal(NpgsqlDbType.Text, byName["externalId"].NpgsqlDbType);
        Assert.Equal(DBNull.Value, byName["vpnServerId"].Value);
        Assert.Equal(DBNull.Value, byName["externalId"].Value);
    }

    [Fact]
    public void BuildFilteredTrafficCte_PlacesOptionalFiltersBeforeOffsetParameter()
    {
        var cte = OverviewTrafficPostgresSql.BuildFilteredTrafficCte(@"""xgb"".""Traffic""");

        var fromIdx = cte.IndexOf("@from", StringComparison.Ordinal);
        var toIdx = cte.IndexOf("@to", StringComparison.Ordinal);
        var serverIdx = cte.IndexOf("@vpnServerId", StringComparison.Ordinal);
        var externalIdx = cte.IndexOf("@externalId", StringComparison.Ordinal);

        Assert.True(fromIdx >= 0);
        Assert.True(toIdx > fromIdx);
        Assert.True(serverIdx > toIdx, "vpnServerId must appear after range params (PG positional $3 when offset is absent in CTE)");
        Assert.True(externalIdx > serverIdx);
        Assert.DoesNotContain("@offset", cte, StringComparison.Ordinal);
    }

    [Fact]
    public void OverviewQueryTestHelper_UsesInMemoryAggregator_NotPostgresSql()
    {
        // Documents why OpenVpnOverview*QueryTests never caught 42P08: no IDbContextFactory => no raw SQL.
        var uow = new Moq.Mock<DataGateMonitor.DataBase.UnitOfWork.IUnitOfWork>().Object;
        var aggregator = OverviewQueryTestHelper.CreateTrafficAggregator(uow);

        Assert.IsType<OverviewTrafficAggregator>(aggregator);
    }
}
