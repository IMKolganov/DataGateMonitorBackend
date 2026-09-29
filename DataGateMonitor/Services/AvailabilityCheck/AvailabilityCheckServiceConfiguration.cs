using DataGateMonitor.Configurations;

namespace DataGateMonitor.Services.AvailabilityCheck;

public static class AvailabilityCheckServiceConfiguration
{
    public static void ConfigureAvailabilityCheckServices(
        this IServiceCollection services,
        DatabaseRuntimeOptions databaseRuntime)
    {
        services.AddHttpClient(AvailabilityCheckProbeClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DataGateMonitor-AvailabilityCheck/1.0");
        });

        services.AddSingleton<IAvailabilityCheckStatusStore, AvailabilityCheckStatusStore>();
        services.AddSingleton<AvailabilityCheckNotificationTracker>();
        services.AddScoped<IAvailabilityCheckProbeClient, AvailabilityCheckProbeClient>();
        services.AddScoped<IAvailabilityCheckNotificationService, AvailabilityCheckNotificationService>();
        services.AddScoped<IAvailabilityCheckRunner, AvailabilityCheckRunner>();

        if (databaseRuntime.IsConnectionConfigured)
            services.AddHostedService<AvailabilityCheckBackgroundService>();
    }
}
