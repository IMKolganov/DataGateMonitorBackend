using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.HealthChecks;

namespace OpenVPNGateMonitor.Configurations;

public static class HealthCheckServicesConfiguration
{
    public static void ConfigureHealthCheckServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(
                connectionString: (builder.Configuration.GetConnectionString("DefaultConnection")
                                   ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE")) ??
                                  throw new InvalidOperationException(
                                      "Could not get DB connection string for health checks"),
                name: "postgresql",
                tags: ["ready"]);
        
        builder.Services.AddHealthChecks()
            .AddCheck<CustomServiceHealthCheck>("custom_service", tags: ["ready"]);
    }
}