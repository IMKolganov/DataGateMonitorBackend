using DataGateMonitor.DataBase.Contexts;
using Microsoft.Extensions.Hosting;

namespace DataGateMonitor.Configurations;

/// <summary>
/// Runs EF wait-for-Postgres and migrations after the host has fully started so HTTP (e.g. Swagger) is available while the database is still down.
/// </summary>
public sealed class EfCoreMigrationHostedService(
    IHostApplicationLifetime lifetime,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EfCoreMigrationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime.ApplicationStarted.Register(
            () =>
            {
                try
                {
                    DatabaseMigrationExtensions.ApplyMigrationsWithDetailedLogging<ApplicationDbContext>(
                        scopeFactory, configuration, logger);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Database migrations failed; stopping the application host.");
                    lifetime.StopApplication();
                }
            });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
