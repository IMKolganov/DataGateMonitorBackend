namespace DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;

/// <summary>
/// PostgreSQL upsert for <c>VpnServerClients</c> on <c>UX_VpnServerClients_Server_Session</c>.
/// </summary>
internal static class VpnServerClientUpsertSql
{
    internal const string TargetAlias = "target";

    internal static string BuildUpsertSql(string qualifiedTable) =>
        $"""
         INSERT INTO {qualifiedTable} AS {TargetAlias}
             ("VpnServerId", "UserId", "ExternalId", "SessionId", "CommonName", "RemoteIp", "ProxyRealIp",
              "LocalIp", "BytesReceived", "BytesSent", "ConnectedSince", "DisconnectedAt", "Username",
              "Country", "Region", "City", "Latitude", "Longitude", "IsConnected", "CreateDate", "LastUpdate")
         VALUES
             (@vpnServerId, @userId, @externalId, @sessionId, @commonName, @remoteIp, @proxyRealIp,
              @localIp, @bytesReceived, @bytesSent, @connectedSince, @disconnectedAt, @username,
              @country, @region, @city, @latitude, @longitude, @isConnected, @now, @now)
         ON CONFLICT ("VpnServerId", "SessionId")
         DO UPDATE SET
             -- overwrite only when incoming value is present (nullable / enriched fields)
             "UserId" = COALESCE(EXCLUDED."UserId", {TargetAlias}."UserId"),
             "ExternalId" = COALESCE(NULLIF(EXCLUDED."ExternalId", ''), {TargetAlias}."ExternalId"),
             "RemoteIp" = COALESCE(NULLIF(EXCLUDED."RemoteIp", ''), {TargetAlias}."RemoteIp"),
             "ProxyRealIp" = COALESCE(EXCLUDED."ProxyRealIp", {TargetAlias}."ProxyRealIp"),
             "LocalIp" = COALESCE(NULLIF(EXCLUDED."LocalIp", ''), {TargetAlias}."LocalIp"),
             "Username" = COALESCE(NULLIF(EXCLUDED."Username", ''), {TargetAlias}."Username"),
             "Country" = COALESCE(EXCLUDED."Country", {TargetAlias}."Country"),
             "Region" = COALESCE(EXCLUDED."Region", {TargetAlias}."Region"),
             "City" = COALESCE(EXCLUDED."City", {TargetAlias}."City"),
             "Latitude" = COALESCE(EXCLUDED."Latitude", {TargetAlias}."Latitude"),
             "Longitude" = COALESCE(EXCLUDED."Longitude", {TargetAlias}."Longitude"),
             -- always overwrite (session identity + live counters)
             "CommonName" = EXCLUDED."CommonName",
             "BytesReceived" = EXCLUDED."BytesReceived",
             "BytesSent" = EXCLUDED."BytesSent",
             "ConnectedSince" = EXCLUDED."ConnectedSince",
             -- special logic (connection lifecycle)
             "IsConnected" = EXCLUDED."IsConnected",
             "DisconnectedAt" = CASE
                 WHEN EXCLUDED."IsConnected" = true THEN NULL
                 ELSE COALESCE(EXCLUDED."DisconnectedAt", {TargetAlias}."DisconnectedAt")
             END,
             "LastUpdate" = EXCLUDED."LastUpdate"
         """;
}
