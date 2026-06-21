using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;

public sealed class VpnServerClientUpsertService(
    ApplicationDbContext dbContext,
    ICommandService<VpnServerClient, int> commandService,
    ILogger<VpnServerClientUpsertService> logger) : IVpnServerClientUpsertService
{
    public async Task<int> UpsertAsync(VpnServerClientUpsertPayload payload, CancellationToken ct = default)
    {
        if (dbContext.Database.IsNpgsql())
            return await UpsertPostgresAsync(payload, ct);

        logger.LogWarning(
            "VpnServerClient upsert using in-memory fallback (non-PostgreSQL provider). " +
            "Production must use Npgsql so INSERT ... ON CONFLICT runs atomically.");
        return await UpsertFallbackAsync(payload, ct);
    }

    private async Task<int> UpsertPostgresAsync(VpnServerClientUpsertPayload payload, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var qualifiedTable = ResolveTable(dbContext);
        var sql = VpnServerClientUpsertSql.BuildUpsertSql(qualifiedTable);

        return await dbContext.Database.ExecuteSqlRawAsync(
            sql,
            BuildParameters(payload, now),
            ct);
    }

    /// <summary>
    /// Check-then-insert fallback for EF in-memory integration tests only.
    /// Not race-safe; production always takes the Npgsql path above.
    /// </summary>
    private async Task<int> UpsertFallbackAsync(VpnServerClientUpsertPayload payload, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var disconnectedAt = payload.IsConnected ? null : payload.DisconnectedAt;

        var rows = await commandService.UpdateWhere(
            x => x.VpnServerId == payload.VpnServerId && x.SessionId == payload.SessionId,
            s => s
                .SetProperty(c => c.UserId, c => payload.UserId ?? c.UserId)
                .SetProperty(c => c.ExternalId, c => string.IsNullOrEmpty(payload.ExternalId) ? c.ExternalId : payload.ExternalId)
                .SetProperty(c => c.CommonName, payload.CommonName)
                .SetProperty(c => c.RemoteIp, c => string.IsNullOrEmpty(payload.RemoteIp) ? c.RemoteIp : payload.RemoteIp)
                .SetProperty(c => c.ProxyRealIp, c => payload.ProxyRealIp ?? c.ProxyRealIp)
                .SetProperty(c => c.LocalIp, c => string.IsNullOrEmpty(payload.LocalIp) ? c.LocalIp : payload.LocalIp)
                .SetProperty(c => c.BytesReceived, payload.BytesReceived)
                .SetProperty(c => c.BytesSent, payload.BytesSent)
                .SetProperty(c => c.ConnectedSince, payload.ConnectedSince)
                .SetProperty(c => c.DisconnectedAt, disconnectedAt)
                .SetProperty(c => c.Username, c => string.IsNullOrEmpty(payload.Username) ? c.Username : payload.Username)
                .SetProperty(c => c.Country, c => payload.Country ?? c.Country)
                .SetProperty(c => c.Region, c => payload.Region ?? c.Region)
                .SetProperty(c => c.City, c => payload.City ?? c.City)
                .SetProperty(c => c.Latitude, c => payload.Latitude ?? c.Latitude)
                .SetProperty(c => c.Longitude, c => payload.Longitude ?? c.Longitude)
                .SetProperty(c => c.IsConnected, payload.IsConnected)
                .SetProperty(c => c.LastUpdate, now),
            ct);

        if (rows > 0)
            return rows;

        var entity = new VpnServerClient
        {
            VpnServerId = payload.VpnServerId,
            UserId = payload.UserId,
            ExternalId = payload.ExternalId,
            SessionId = payload.SessionId,
            CommonName = payload.CommonName,
            RemoteIp = payload.RemoteIp,
            ProxyRealIp = payload.ProxyRealIp,
            LocalIp = payload.LocalIp,
            BytesReceived = payload.BytesReceived,
            BytesSent = payload.BytesSent,
            ConnectedSince = payload.ConnectedSince,
            DisconnectedAt = payload.DisconnectedAt,
            Username = payload.Username,
            Country = payload.Country,
            Region = payload.Region,
            City = payload.City,
            Latitude = payload.Latitude,
            Longitude = payload.Longitude,
            IsConnected = payload.IsConnected,
            CreateDate = now,
            LastUpdate = now,
        };

        await commandService.Add(entity, saveChanges: true, ct);
        return 1;
    }

    private static NpgsqlParameter[] BuildParameters(VpnServerClientUpsertPayload payload, DateTimeOffset now) =>
    [
        new NpgsqlParameter("vpnServerId", NpgsqlDbType.Integer) { Value = payload.VpnServerId },
        new NpgsqlParameter("userId", NpgsqlDbType.Integer) { Value = (object?)payload.UserId ?? DBNull.Value },
        new NpgsqlParameter("externalId", NpgsqlDbType.Varchar) { Value = payload.ExternalId },
        new NpgsqlParameter("sessionId", NpgsqlDbType.Uuid) { Value = payload.SessionId },
        new NpgsqlParameter("commonName", NpgsqlDbType.Varchar) { Value = payload.CommonName },
        new NpgsqlParameter("remoteIp", NpgsqlDbType.Varchar) { Value = payload.RemoteIp },
        new NpgsqlParameter("proxyRealIp", NpgsqlDbType.Varchar) { Value = (object?)payload.ProxyRealIp ?? DBNull.Value },
        new NpgsqlParameter("localIp", NpgsqlDbType.Varchar) { Value = payload.LocalIp },
        new NpgsqlParameter("bytesReceived", NpgsqlDbType.Bigint) { Value = payload.BytesReceived },
        new NpgsqlParameter("bytesSent", NpgsqlDbType.Bigint) { Value = payload.BytesSent },
        new NpgsqlParameter("connectedSince", NpgsqlDbType.TimestampTz) { Value = payload.ConnectedSince.UtcDateTime },
        new NpgsqlParameter("disconnectedAt", NpgsqlDbType.TimestampTz)
        {
            Value = payload.DisconnectedAt.HasValue ? payload.DisconnectedAt.Value.UtcDateTime : DBNull.Value
        },
        new NpgsqlParameter("username", NpgsqlDbType.Varchar) { Value = payload.Username },
        new NpgsqlParameter("country", NpgsqlDbType.Varchar) { Value = (object?)payload.Country ?? DBNull.Value },
        new NpgsqlParameter("region", NpgsqlDbType.Varchar) { Value = (object?)payload.Region ?? DBNull.Value },
        new NpgsqlParameter("city", NpgsqlDbType.Varchar) { Value = (object?)payload.City ?? DBNull.Value },
        new NpgsqlParameter("latitude", NpgsqlDbType.Double) { Value = (object?)payload.Latitude ?? DBNull.Value },
        new NpgsqlParameter("longitude", NpgsqlDbType.Double) { Value = (object?)payload.Longitude ?? DBNull.Value },
        new NpgsqlParameter("isConnected", NpgsqlDbType.Boolean) { Value = payload.IsConnected },
        new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now.UtcDateTime },
    ];

    private static string ResolveTable(ApplicationDbContext ctx)
    {
        var entity = ctx.Model.FindEntityType(typeof(VpnServerClient))
                     ?? throw new InvalidOperationException($"{nameof(VpnServerClient)} is not mapped.");
        var schema = entity.GetSchema() ?? "public";
        var table = entity.GetTableName() ?? throw new InvalidOperationException("Table name missing.");
        return $"\"{schema}\".\"{table}\"";
    }
}
