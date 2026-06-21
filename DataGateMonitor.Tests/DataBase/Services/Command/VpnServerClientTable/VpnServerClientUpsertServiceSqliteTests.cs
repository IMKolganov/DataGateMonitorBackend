using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories;
using DataGateMonitor.DataBase.Repositories.Queries;
using DataGateMonitor.DataBase.Services.Command;
using DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;

namespace DataGateMonitor.Tests.DataBase.Services.Command.VpnServerClientTable;

/// <summary>
/// Exercises <see cref="VpnServerClientUpsertService"/> merge semantics via the SQLite fallback path
/// (non-Npgsql). Production uses PostgreSQL <c>INSERT ... ON CONFLICT</c> instead.
/// </summary>
public sealed class VpnServerClientUpsertServiceSqliteTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private IVpnServerClientUpsertService _sut = null!;

    public async Task InitializeAsync()
    {
        (_connection, _context, _sut) = await VpnServerClientUpsertTestHarness.CreateSqliteAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task UpsertAsync_inserts_when_row_missing()
    {
        var sessionId = Guid.NewGuid();
        var payload = VpnServerClientUpsertTestHarness.CreatePayload(sessionId, externalId: "ext-1");

        var affected = await _sut.UpsertAsync(payload);

        Assert.Equal(1, affected);
        var row = await _context.VpnServerClients.AsNoTracking().SingleAsync(c => c.SessionId == sessionId);
        Assert.Equal("ext-1", row.ExternalId);
        Assert.True(row.IsConnected);
    }

    [Fact]
    public async Task UpsertAsync_updates_existing_row_without_duplicate()
    {
        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-5);

        await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "ext-known", bytesReceived: 100, connectedSince: connectedSince));

        await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "", bytesReceived: 500, connectedSince: connectedSince));

        var rows = await _context.VpnServerClients.AsNoTracking()
            .Where(c => c.SessionId == sessionId).ToListAsync();
        Assert.Single(rows);
        Assert.Equal("ext-known", rows[0].ExternalId);
        Assert.Equal(500, rows[0].BytesReceived);
    }

    [Fact]
    public async Task UpsertAsync_does_not_wipe_enriched_fields_when_incoming_null()
    {
        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-3);

        await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId,
            externalId: "ext-geo",
            connectedSince: connectedSince,
            country: "DE",
            proxyRealIp: "203.0.113.1:443"));

        await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId,
            externalId: "ext-geo",
            connectedSince: connectedSince,
            country: null,
            proxyRealIp: null,
            bytesReceived: 999));

        var row = await _context.VpnServerClients.AsNoTracking().SingleAsync(c => c.SessionId == sessionId);
        Assert.Equal("DE", row.Country);
        Assert.Equal("203.0.113.1:443", row.ProxyRealIp);
        Assert.Equal(999, row.BytesReceived);
    }

    [Fact]
    public async Task UpsertAsync_clears_disconnected_at_when_client_is_connected()
    {
        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-2);
        var disconnectedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        _context.VpnServerClients.Add(new VpnServerClient
        {
            VpnServerId = VpnServerClientUpsertTestHarness.TestVpnServerId,
            SessionId = sessionId,
            ExternalId = "ext-dc",
            CommonName = "cn",
            RemoteIp = "1.2.3.4",
            LocalIp = "10.8.0.2",
            BytesReceived = 0,
            BytesSent = 0,
            ConnectedSince = connectedSince,
            DisconnectedAt = disconnectedAt,
            Username = "cn",
            IsConnected = false,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        });
        await _context.SaveChangesAsync();

        await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "ext-dc", connectedSince: connectedSince, isConnected: true));

        var row = await _context.VpnServerClients.AsNoTracking().SingleAsync(c => c.SessionId == sessionId);
        Assert.True(row.IsConnected);
        Assert.Null(row.DisconnectedAt);
    }

    [Fact]
    public async Task UpsertAsync_preserves_create_date_on_update()
    {
        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-10);

        await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "ext-cd", connectedSince: connectedSince));

        var createDate = (await _context.VpnServerClients.AsNoTracking().SingleAsync(c => c.SessionId == sessionId)).CreateDate;

        await Task.Delay(15);
        await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "ext-cd", connectedSince: connectedSince, bytesReceived: 42));

        var row = await _context.VpnServerClients.AsNoTracking().SingleAsync(c => c.SessionId == sessionId);
        Assert.Equal(createDate, row.CreateDate);
        Assert.True(row.LastUpdate >= createDate);
    }
}
