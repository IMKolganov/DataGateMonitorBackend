using DataGateMonitor.Services.HealthChecks;

namespace DataGateMonitor.Configurations;

public static class HealthCheckServicesConfiguration
{
    public static void ConfigureHealthCheckServices(this IServiceCollection services, DatabaseRuntimeOptions databaseRuntime)
    {
        var healthChecks = services.AddHealthChecks();
        if (databaseRuntime.IsConnectionConfigured && !string.IsNullOrWhiteSpace(databaseRuntime.ConfiguredConnectionString))
        {
            healthChecks.AddNpgSql(databaseRuntime.ConfiguredConnectionString, name: "postgresql", tags: ["ready"]);
        }

        healthChecks.AddCheck<CustomServiceHealthCheck>("custom_service", tags: ["ready"]);
    }
}