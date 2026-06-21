using DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;

namespace DataGateMonitor.Tests.DataBase.Services.Command.VpnServerClientTable;

public class VpnServerClientUpsertPostgresSqlTests
{
    [Fact]
    public void BuildUpsertSql_UsesTargetAliasAndOnConflictOnServerAndSession()
    {
        var sql = VpnServerClientUpsertSql.BuildUpsertSql(@"""xgb"".""VpnServerClients""");

        Assert.Contains(@"""xgb"".""VpnServerClients"" AS target", sql, StringComparison.Ordinal);
        Assert.Contains(@"ON CONFLICT (""VpnServerId"", ""SessionId"")", sql, StringComparison.Ordinal);
        Assert.Contains("DO UPDATE SET", sql, StringComparison.Ordinal);
        Assert.Contains(@"target.""UserId""", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(@"""VpnServerClients"".""UserId""", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUpsertSql_PreservesCreateDateOnConflict()
    {
        var sql = VpnServerClientUpsertSql.BuildUpsertSql(@"""public"".""VpnServerClients""");

        Assert.Contains(@"""CreateDate""", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(@"""CreateDate"" = EXCLUDED.""CreateDate""", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUpsertSql_MergesEnrichedAndNullableFieldsWithoutWiping()
    {
        var sql = VpnServerClientUpsertSql.BuildUpsertSql(@"""public"".""VpnServerClients""");

        Assert.Contains(@"COALESCE(EXCLUDED.""ProxyRealIp"", target.""ProxyRealIp"")", sql, StringComparison.Ordinal);
        Assert.Contains(@"COALESCE(EXCLUDED.""Country"", target.""Country"")", sql, StringComparison.Ordinal);
        Assert.Contains(@"COALESCE(EXCLUDED.""UserId"", target.""UserId"")", sql, StringComparison.Ordinal);
        Assert.Contains(@"COALESCE(NULLIF(EXCLUDED.""ExternalId"", ''), target.""ExternalId"")", sql, StringComparison.Ordinal);
        Assert.Contains(@"COALESCE(NULLIF(EXCLUDED.""RemoteIp"", ''), target.""RemoteIp"")", sql, StringComparison.Ordinal);
        Assert.Contains(@"COALESCE(NULLIF(EXCLUDED.""LocalIp"", ''), target.""LocalIp"")", sql, StringComparison.Ordinal);
        Assert.Contains(@"COALESCE(NULLIF(EXCLUDED.""Username"", ''), target.""Username"")", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUpsertSql_ClearsDisconnectedAtWhenClientIsConnected()
    {
        var sql = VpnServerClientUpsertSql.BuildUpsertSql(@"""public"".""VpnServerClients""");

        Assert.Contains(@"WHEN EXCLUDED.""IsConnected"" = true THEN NULL", sql, StringComparison.Ordinal);
        Assert.Contains(@"COALESCE(EXCLUDED.""DisconnectedAt"", target.""DisconnectedAt"")", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUpsertSql_AlwaysOverwritesLiveCountersAndConnectionState()
    {
        var sql = VpnServerClientUpsertSql.BuildUpsertSql(@"""public"".""VpnServerClients""");

        Assert.Contains(@"""BytesReceived"" = EXCLUDED.""BytesReceived""", sql, StringComparison.Ordinal);
        Assert.Contains(@"""BytesSent"" = EXCLUDED.""BytesSent""", sql, StringComparison.Ordinal);
        Assert.Contains(@"""IsConnected"" = EXCLUDED.""IsConnected""", sql, StringComparison.Ordinal);
        Assert.Contains(@"""LastUpdate"" = EXCLUDED.""LastUpdate""", sql, StringComparison.Ordinal);
    }
}
