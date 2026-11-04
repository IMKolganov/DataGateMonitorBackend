using DataGateMonitor.DataBase.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DataGateMonitor.Tests.Services.LocalIssuance;

/// <summary>Opt-in PostgreSQL check of the incremental migration against an existing table, not the historical fresh-DB chain.</summary>
public sealed class RemoveLocalProfileStaticIpPostgresTests
{
    private const string Previous = "20261028163633_LocalIssuance_NodeKeys_IssuedOvpnFileOrigin";
    private const string Current = "20261028223008_RemoveLocalProfileStaticIp";

    [SkippableFact]
    public async Task DropColumn_UpgradeAndDowngrade_PreserveProfiles()
    {
        var connection = Environment.GetEnvironmentVariable("DATAGATE_MIGRATION_TEST_PG_CONNECTION");
        Skip.If(string.IsNullOrWhiteSpace(connection), "Set DATAGATE_MIGRATION_TEST_PG_CONNECTION to an isolated PostgreSQL server.");
        var database = "dgm_lookup_migration_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {database}", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(connection) { Database = database, Pooling = false };
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataBaseSettings:DefaultSchema"] = "xgb_dashopnvpn",
            }).Build();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(cs.ConnectionString,
                pg => pg.MigrationsHistoryTable("__EFMigrationsHistory", "xgb_dashopnvpn")).Options;
            await using var db = new ApplicationDbContext(options, config);
            // Minimal existing table isolates this DropColumn from unrelated historical migration issues.
            await db.Database.ExecuteSqlRawAsync("""
                CREATE SCHEMA xgb_dashopnvpn;
                CREATE TABLE xgb_dashopnvpn."IssuedOvpnFiles" (
                    "Id" integer PRIMARY KEY, "CommonName" text NOT NULL,
                    "IsServiceProfile" boolean NOT NULL, "StaticIp" varchar(45));
                INSERT INTO xgb_dashopnvpn."IssuedOvpnFiles" VALUES
                    (1, 'service-client', true, '10.89.94.2'), (2, 'user-client', false, NULL);
                CREATE TABLE xgb_dashopnvpn."__EFMigrationsHistory" (
                    "MigrationId" varchar(150) PRIMARY KEY, "ProductVersion" varchar(32) NOT NULL);
                INSERT INTO xgb_dashopnvpn."__EFMigrationsHistory" VALUES
                    ('20261028163633_LocalIssuance_NodeKeys_IssuedOvpnFileOrigin', '10.0.13');
                """);

            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Current);
            await db.Database.ExecuteSqlRawAsync(up);
            Assert.Equal(0, await ColumnCountAsync(cs.ConnectionString));
            await AssertProfilesAsync(cs.ConnectionString);

            var down = migrator.GenerateScript(Current, Previous);
            await db.Database.ExecuteSqlRawAsync(down);
            Assert.Equal(1, await ColumnCountAsync(cs.ConnectionString));
            await AssertProfilesAsync(cs.ConnectionString);

            // Run the upgrade again as an operator would after a rollback.
            await db.Database.ExecuteSqlRawAsync(up);
            Assert.Equal(0, await ColumnCountAsync(cs.ConnectionString));
            await AssertProfilesAsync(cs.ConnectionString);
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<int> ColumnCountAsync(string cs)
    {
        await using var connection = new NpgsqlConnection(cs);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT count(*) FROM information_schema.columns
            WHERE table_schema='xgb_dashopnvpn' AND table_name='IssuedOvpnFiles' AND column_name='StaticIp'
            """, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task AssertProfilesAsync(string cs)
    {
        await using var connection = new NpgsqlConnection(cs);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT "Id", "CommonName", "IsServiceProfile" FROM xgb_dashopnvpn."IssuedOvpnFiles" ORDER BY "Id"
            """, connection);
        await using var rows = await command.ExecuteReaderAsync();
        Assert.True(await rows.ReadAsync());
        Assert.Equal((1, "service-client", true), (rows.GetInt32(0), rows.GetString(1), rows.GetBoolean(2)));
        Assert.True(await rows.ReadAsync());
        Assert.Equal((2, "user-client", false), (rows.GetInt32(0), rows.GetString(1), rows.GetBoolean(2)));
        Assert.False(await rows.ReadAsync());
    }
}
