using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories;
using DataGateMonitor.DataBase.Repositories.Queries;
using DataGateMonitor.DataBase.Services.Command;
using DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;
using DataGateMonitor.DataBase.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DataGateMonitor.Tests.DataBase.Services.Command.VpnServerClientTable;

/// <summary>
/// PostgreSQL integration tests for atomic upsert and concurrent writers.
/// Skipped unless <c>DATAGATE_TEST_PG_CONNECTION</c> is set and the server is reachable.
/// Example: DATAGATE_TEST_PG_CONNECTION="Host=localhost;Port=5433;Database=datagate_backend;Username=backend_user;Password=..."
/// </summary>
public sealed class VpnServerClientUpsertPostgresIntegrationTests : IAsyncLifetime
{
    private string? _connectionString;
    private IConfiguration? _configuration;
    private ApplicationDbContext? _probeContext;
    private bool _postgresAvailable;
    private string _skipReason =
        "Set DATAGATE_TEST_PG_CONNECTION to run PostgreSQL upsert integration tests.";

    public async Task InitializeAsync()
    {
        _connectionString = Environment.GetEnvironmentVariable("DATAGATE_TEST_PG_CONNECTION");
        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        try
        {
            await using var probe = new NpgsqlConnection(_connectionString);
            await probe.OpenAsync();

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DataBaseSettings:DefaultSchema"] = "xgb_dashopnvpn",
                })
                .Build();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_connectionString)
                .Options;

            _probeContext = new ApplicationDbContext(options, _configuration);

            var schemaExists = await _probeContext.VpnServerClients.AsNoTracking().AnyAsync();
            if (!schemaExists)
                await _probeContext.Database.MigrateAsync();

            _postgresAvailable = true;
        }
        catch (Exception ex)
        {
            _postgresAvailable = false;
            _skipReason = $"PostgreSQL integration tests unavailable: {ex.Message}";
            if (_probeContext is not null)
                await _probeContext.DisposeAsync();
            _probeContext = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (_probeContext is not null)
            await _probeContext.DisposeAsync();
    }

    [SkippableFact]
    public async Task UpsertAsync_on_postgres_uses_on_conflict_not_duplicate_insert()
    {
        RequirePostgres();

        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-7);
        var payload = VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "pg-ext", connectedSince: connectedSince, bytesReceived: 10);

        await using var ctx = CreateContext();
        var sut = CreateUpsertService(ctx);

        try
        {
            await sut.UpsertAsync(payload);
            await sut.UpsertAsync(payload with { BytesReceived = 20, ExternalId = "" });

            var rows = await ctx.VpnServerClients
                .AsNoTracking()
                .Where(c => c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId)
                .ToListAsync();

            Assert.Single(rows);
            Assert.Equal("pg-ext", rows[0].ExternalId);
            Assert.Equal(20, rows[0].BytesReceived);
        }
        finally
        {
            await ctx.VpnServerClients
                .Where(c => c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId)
                .ExecuteDeleteAsync();
        }
    }

    [SkippableFact]
    public async Task UpsertAsync_concurrent_writers_same_session_produce_single_row()
    {
        RequirePostgres();

        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-4);
        var payload = VpnServerClientUpsertTestHarness.CreatePayload(
            sessionId, externalId: "race-ext", connectedSince: connectedSince);

        await using var verifyContext = CreateContext();

        try
        {
            var tasks = Enumerable.Range(0, 32)
                .Select(async i =>
                {
                    await using var ctx = CreateContext();
                    var sut = CreateUpsertService(ctx);
                    await sut.UpsertAsync(payload with { BytesReceived = i * 100L });
                })
                .ToArray();

            await Task.WhenAll(tasks);

            var count = await verifyContext.VpnServerClients.AsNoTracking().CountAsync(c =>
                c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId);

            Assert.Equal(1, count);
        }
        finally
        {
            await verifyContext.VpnServerClients
                .Where(c => c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId)
                .ExecuteDeleteAsync();
        }
    }

    [SkippableFact]
    public async Task UpsertAsync_on_postgres_clears_disconnected_at_when_connected()
    {
        RequirePostgres();

        var sessionId = Guid.NewGuid();
        var connectedSince = DateTimeOffset.UtcNow.AddMinutes(-6);

        await using var ctx = CreateContext();
        var sut = CreateUpsertService(ctx);

        try
        {
            await sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
                sessionId,
                externalId: "pg-dc",
                connectedSince: connectedSince,
                isConnected: false,
                disconnectedAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

            await sut.UpsertAsync(VpnServerClientUpsertTestHarness.CreatePayload(
                sessionId,
                externalId: "pg-dc",
                connectedSince: connectedSince,
                isConnected: true,
                disconnectedAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

            var row = await ctx.VpnServerClients.AsNoTracking().SingleAsync(c =>
                c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId);

            Assert.True(row.IsConnected);
            Assert.Null(row.DisconnectedAt);
        }
        finally
        {
            await ctx.VpnServerClients
                .Where(c => c.VpnServerId == VpnServerClientUpsertTestHarness.TestVpnServerId && c.SessionId == sessionId)
                .ExecuteDeleteAsync();
        }
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString!)
            .Options;
        return new ApplicationDbContext(options, _configuration!);
    }

    private static IVpnServerClientUpsertService CreateUpsertService(ApplicationDbContext context)
    {
        var repositoryFactory = new RepositoryFactory(context);
        var queryFactory = new QueryFactory(context);
        var dbContextFactory = new Moq.Mock<IDbContextFactory<ApplicationDbContext>>();
        var unitOfWork = new UnitOfWork(context, dbContextFactory.Object, repositoryFactory, queryFactory);
        var commandService = new EfCommandService<Models.VpnServerClient, int>(unitOfWork);
        return new VpnServerClientUpsertService(context, commandService, NullLogger<VpnServerClientUpsertService>.Instance);
    }

    private void RequirePostgres()
    {
        Skip.IfNot(_postgresAvailable, _skipReason);
        Assert.NotNull(_connectionString);
        Assert.NotNull(_configuration);
    }
}
