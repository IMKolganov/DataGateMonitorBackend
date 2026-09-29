using DataGateMonitor.Configurations;

namespace DataGateMonitor.Services.RfAvailability;

public static class RfAvailabilityServiceConfiguration
{
    public static void ConfigureRfAvailabilityServices(
        this IServiceCollection services,
        DatabaseRuntimeOptions databaseRuntime)
    {
        services.AddHttpClient(RfAvailabilityProbeClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DataGateMonitor-RfAvailability/1.0");
        });

        services.AddSingleton<IRfAvailabilityStatusStore, RfAvailabilityStatusStore>();
        services.AddSingleton<RfAvailabilityNotificationTracker>();
        services.AddScoped<IRfAvailabilityProbeClient, RfAvailabilityProbeClient>();
        services.AddScoped<IRfAvailabilityNotificationService, RfAvailabilityNotificationService>();
        services.AddScoped<IRfAvailabilityCheckRunner, RfAvailabilityCheckRunner>();

        if (databaseRuntime.IsConnectionConfigured)
            services.AddHostedService<RfAvailabilityBackgroundService>();
    }
}
