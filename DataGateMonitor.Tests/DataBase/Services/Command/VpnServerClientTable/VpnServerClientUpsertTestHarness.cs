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

namespace DataGateMonitor.Tests.DataBase.Services.Command.VpnServerClientTable;

internal static class VpnServerClientUpsertTestHarness
{
    internal const int TestVpnServerId = 9_999_001;

    internal static VpnServerClientUpsertPayload CreatePayload(
        Guid sessionId,
        string externalId = "ext-default",
        DateTimeOffset? connectedSince = null,
        long bytesReceived = 0,
        bool isConnected = true,
        DateTimeOffset? disconnectedAt = null,
        string? country = null,
        string? proxyRealIp = null) =>
        new(
            VpnServerId: TestVpnServerId,
            UserId: null,
            ExternalId: externalId,
            SessionId: sessionId,
            CommonName: "test-cn",
            RemoteIp: "198.51.100.10",
            ProxyRealIp: proxyRealIp,
            LocalIp: "10.8.0.5",
            BytesReceived: bytesReceived,
            BytesSent: 0,
            ConnectedSince: connectedSince ?? DateTimeOffset.UtcNow,
            DisconnectedAt: disconnectedAt,
            Username: "test-cn",
            Country: country,
            Region: null,
            City: null,
            Latitude: null,
            Longitude: null,
            IsConnected: isConnected);

    internal static async Task<(SqliteConnection Connection, ApplicationDbContext Context, IVpnServerClientUpsertService Sut)>
        CreateSqliteAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(b => b.Ignore(RelationalEventId.AmbientTransactionWarning))
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataBaseSettings:DefaultSchema"] = "test_schema",
            })
            .Build();

        var context = new ApplicationDbContext(options, configuration);
        await context.Database.EnsureCreatedAsync();

        var repositoryFactory = new RepositoryFactory(context);
        var queryFactory = new QueryFactory(context);
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        var unitOfWork = new UnitOfWork(context, dbContextFactory.Object, repositoryFactory, queryFactory);
        var commandService = new EfCommandService<Models.VpnServerClient, int>(unitOfWork);
        var sut = new VpnServerClientUpsertService(context, commandService, NullLogger<VpnServerClientUpsertService>.Instance);

        return (connection, context, sut);
    }
}
