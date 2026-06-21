using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories;
using DataGateMonitor.DataBase.Repositories.Queries;
using DataGateMonitor.DataBase.Services.Command;
using DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace DataGateMonitor.Tests.DataBase.Services.Command.VpnServerClientTable;

/// <summary>
/// PostgreSQL integration tests for atomic upsert and concurrent writers.
/// Skipped when <c>DATAGATE_TEST_PG_CONNECTION</c> is unset or the server is unreachable.
/// Example: DATAGATE_TEST_PG_CONNECTION="Host=localhost;Port=5432;Database=datagate_local;Username=postgres;Password=dgh@14f!"
/// </summary>
public sealed class VpnServerClientUpsertPostgresIntegrationTests : IAsyncLifetime
{
    private ApplicationDbContext? _context;
    private IVpnServerClientUpsertService? _sut;
    private bool _postgresAvailable;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("DATAGATE_TEST_PG_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        try
        {
            await using var probe = new NpgsqlConnection(connectionString);
            await probe.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DataBaseSettings:DefaultSchema"] = "xgb_dashopnvpn",
                })
                .Build();

            _context = new ApplicationDbContext(options, configuration);
            await _context.Database.MigrateAsync();

            var repositoryFactory = new RepositoryFactory(_context);
            var queryFactory = new QueryFactory(_context);
            var dbContextFactory = new Moq.Mock<IDbContextFactory<ApplicationDbContext>>();
            var unitOfWork = new UnitOfWork(_context, dbContextFactory.Object, repositoryFactory, queryFactory);
            var commandService = new EfCommandService<VpnServerClient, int>(unitOfWork);
            _sut = new VpnServerClientUpsertService(_context, commandService, NullLogger<VpnServerClientUpsertService>.Instance);
            _postgresAvailable = true;
        }
        catch
        {
            _postgresAvailable = false;
            if (_context is not null)
                await _context.DisposeAsync();
            _context = null;
            _sut = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (_context is not null)
            await _context.DisposeAsync();
    }

    [Fact]
    public async Task UpsertAsync_on_postgres_uses_on_conflict_not_duplicate_insert()
    {
        if (!_postgresAvailable || _context is null || _sut is null)
            return;

        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-7);
        var payload = VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "pg-ext", connectedSince: connectedSince, bytesReceived: 10);

        try
        {
            await _sut.UpsertAsync(payload);
            await _sut.UpsertAsync(payload with { BytesReceived = 20, ExternalId = "" });

            var rows = await _context.VpnServerClients
                .Where(c => c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId)
                .ToListAsync();

            Assert.Single(rows);
            Assert.Equal("pg-ext", rows[0].ExternalId);
            Assert.Equal(20, rows[0].BytesReceived);
        }
        finally
        {
            await _context.VpnServerClients
                .Where(c => c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task UpsertAsync_concurrent_writers_same_session_produce_single_row()
    {
        if (!_postgresAvailable || _context is null || _sut is null)
            return;

        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-4);
        var payload = VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "race-ext", connectedSince: connectedSince);

        try
        {
            var tasks = Enumerable.Range(0, 32)
                .Select(i => _sut.UpsertAsync(payload with { BytesReceived = i * 100L }))
                .ToArray();

            await Task.WhenAll(tasks);

            var count = await _context.VpnServerClients.CountAsync(c =>
                c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId);

            Assert.Equal(1, count);
        }
        finally
        {
            await _context.VpnServerClients
                .Where(c => c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task UpsertAsync_on_postgres_clears_disconnected_at_when_connected()
    {
        if (!_postgresAvailable || _context is null || _sut is null)
            return;

        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-6);

        try
        {
            await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
                sessionId,
                externalId: "pg-dc",
                connectedSince: connectedSince,
                isConnected: false,
                disconnectedAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

            await _sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
                sessionId,
                externalId: "pg-dc",
                connectedSince: connectedSince,
                isConnected: true,
                disconnectedAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

            var row = await _context.VpnServerClients.SingleAsync(c =>
                c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId);

            Assert.True(row.IsConnected);
            Assert.Null(row.DisconnectedAt);
        }
        finally
        {
            await _context.VpnServerClients
                .Where(c => c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId)
                .ExecuteDeleteAsync();
        }
    }
}
