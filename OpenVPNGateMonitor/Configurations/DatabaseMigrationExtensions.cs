using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace OpenVPNGateMonitor.Configurations;

public static class DatabaseMigrationExtensions
{
    public static void ApplyMigrationsWithDetailedLogging<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

        try
        {
            var pending = dbContext.Database.GetPendingMigrations().ToList();
            if (!pending.Any())
            {
                app.Logger.LogInformation("Database is up-to-date. No pending migrations.");
                return;
            }

            app.Logger.LogInformation("Applying {Count} pending migrations: {List}",
                pending.Count, string.Join(", ", pending));

            var strategy = dbContext.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                var migrator = dbContext.Database.GetService<IMigrator>();

                foreach (var migration in pending)
                {
                    var sw = Stopwatch.StartNew();
                    app.Logger.LogInformation("Applying migration: {Migration}", migration);

                    try
                    {
                        // No explicit transaction here — EF handles it during migrations
                        migrator.Migrate(migration);

                        sw.Stop();
                        app.Logger.LogInformation("Migration applied: {Migration} in {Elapsed} ms",
                            migration, sw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();

                        var pg = FindPostgresException(ex);
                        if (pg is not null)
                        {
                            app.Logger.LogError(ex,
                                "Migration FAILED: {Migration}. PostgresSQL SqlState={SqlState}, Message={MessageText}, Severity={Severity}, Routine={Routine}",
                                migration, pg.SqlState, pg.MessageText, pg.Severity, pg.Routine);
                        }
                        else
                        {
                            app.Logger.LogError(ex, "Migration FAILED: {Migration}. {Message}",
                                migration, ex.Message);
                        }

                        throw new InvalidOperationException($"Migration failed: {migration}", ex);
                    }
                }
            });

            app.Logger.LogInformation("All pending migrations applied successfully.");
        }
        catch (Exception ex)
        {
            var pg = FindPostgresException(ex);
            if (pg is not null)
            {
                app.Logger.LogError(ex,
                    "An error occurred while applying migrations. PostgresSQL SqlState={SqlState}, Message={MessageText}",
                    pg.SqlState, pg.MessageText);
            }
            else
            {
                app.Logger.LogError(ex, "An error occurred while applying migrations: {Message}", ex.Message);
            }
            throw;
        }

        static PostgresException? FindPostgresException(Exception ex)
        {
            var cur = ex;
            while (cur != null)
            {
                if (cur is PostgresException pge)
                    return pge;

                cur = cur.InnerException;
            }

            return null;
        }
    }
}
