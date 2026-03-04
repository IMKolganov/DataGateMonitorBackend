using OpenVPNGateMonitor.Services.HealthChecks;

namespace OpenVPNGateMonitor.Configurations;

public static class HealthCheckServicesConfiguration
{
    public static void ConfigureHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: (configuration.GetConnectionString("DefaultConnection")
                                   ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE")) ??
                                  throw new InvalidOperationException(
                                      "Could not get DB connection string for health checks"),
                name: "postgresql",
                tags: ["ready"]);
        
        services.AddHealthChecks()
            .AddCheck<CustomServiceHealthCheck>("custom_service", tags: ["ready"]);
    }
}