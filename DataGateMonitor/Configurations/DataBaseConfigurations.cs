using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories;
using DataGateMonitor.DataBase.Repositories.Interfaces;
using DataGateMonitor.DataBase.Repositories.Queries;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ILogger = Serilog.ILogger;

namespace DataGateMonitor.Configurations;

public static class DataBaseConfigurations
{
    public static void DataBaseServices(this IServiceCollection services, IConfiguration configuration,
        ILogger logger, DatabaseRuntimeOptions databaseRuntime)
    {
        if (!databaseRuntime.IsConnectionConfigured)
        {
            logger.Error(
                "Database connection string is missing. Attempted to read from environment variable " +
                "'DB_CONNECTION_STRING_DATAGATE' or configuration key 'ConnectionStrings:DefaultConnection'. " +
                "The API will start without a real database; configure a connection string and restart.");
        }

        var connectionString = databaseRuntime.EfConnectionString;

        try
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            if (databaseRuntime.IsConnectionConfigured)
            {
                logger.Information("Using PostgreSQL Database. Host: {Host}, Port: {Port}, Database: {Database}",
                    builder.Host, builder.Port, builder.Database);
            }
            else
            {
                logger.Information(
                    "EF Core registered with placeholder connection (no real DB). Host: {Host}, Port: {Port}, Database: {Database}",
                    builder.Host, builder.Port, builder.Database);
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to parse connection string for logging.");
        }

        var dbSettings = new DataBaseSettings
        {
            DefaultSchema = Environment.GetEnvironmentVariable("DB_DEFAULT_SCHEMA") 
                            ?? configuration["DataBaseSettings:DefaultSchema"],

            MigrationTable = Environment.GetEnvironmentVariable("DB_MIGRATION_TABLE") 
                             ?? configuration["DataBaseSettings:MigrationTable"]
        };

        // Scoped ApplicationDbContext
        services.AddDbContext<ApplicationDbContext>((options) =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    dbSettings.MigrationTable ?? "__EFMigrationsHistory",
                    dbSettings.DefaultSchema ?? "public"
                )
            );
            
            options.LogTo(_ => { }, LogLevel.Warning);
        }, ServiceLifetime.Scoped);

        // Scoped DbContextFactory
        services.AddDbContextFactory<ApplicationDbContext>((options) =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    dbSettings.MigrationTable ?? "__EFMigrationsHistory",
                    dbSettings.DefaultSchema ?? "public"
                )
            );
            
            options.LogTo(_ => { }, LogLevel.Warning);
        }, ServiceLifetime.Scoped);
        
        services.AddScoped<IRepositoryFactory, RepositoryFactory>();
        services.AddScoped<IQueryFactory, QueryFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<ApplicationDatabaseState>();
        services.AddSingleton<IApplicationDatabaseState>(sp => sp.GetRequiredService<ApplicationDatabaseState>());
        if (databaseRuntime.IsConnectionConfigured)
            services.AddHostedService<EfCoreMigrationHostedService>();
        else
            services.AddHostedService<MarkDatabaseUnconfiguredHostedService>();
    }
}

/// <summary>
/// When no connection string was provided, skip migrations and mark DB state so the root/status line is honest.
/// </summary>
file sealed class MarkDatabaseUnconfiguredHostedService(ApplicationDatabaseState databaseState) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        databaseState.SetFailed(
            "connection string not configured (see startup log for DB_CONNECTION_STRING_DATAGATE / ConnectionStrings:DefaultConnection)");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}