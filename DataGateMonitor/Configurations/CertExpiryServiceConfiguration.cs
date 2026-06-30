using DataGateMonitor.Services.CertExpiry;

namespace DataGateMonitor.Configurations;

public static class CertExpiryServiceConfiguration
{
    public static void ConfigureCertExpiryServices(this IServiceCollection services, DatabaseRuntimeOptions databaseRuntime)
    {
        services.AddSingleton<CertExpiryNotificationTracker>();
        services.AddScoped<ICertExpiryScheduledCheckRunner, CertExpiryScheduledCheckRunner>();

        if (databaseRuntime.IsConnectionConfigured)
            services.AddHostedService<CertExpiryBackgroundService>();
    }
}
