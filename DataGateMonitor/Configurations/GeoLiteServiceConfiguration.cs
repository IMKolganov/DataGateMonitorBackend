using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.GeoLite.Interfaces;

namespace DataGateMonitor.Configurations;

public static class GeoLiteServiceConfiguration
{
    public static void ConfigureGeoLiteServices(this IServiceCollection services, DatabaseRuntimeOptions databaseRuntime)
    {
        // Singleton database factory (shared across app)
        services.AddSingleton<GeoLiteDatabaseFactory>();

        // Scoped query service
        services.AddScoped<IGeoLiteQueryService, GeoLiteQueryService>();

        // Updater singleton (used by background tasks / endpoints)
        services.AddSingleton<GeoLiteUpdaterService>();
        services.AddSingleton<IGeoLiteUpdaterService>(p => p.GetRequiredService<GeoLiteUpdaterService>());

        // HttpClient factory for the updater
        services.AddHttpClient<IGeoLiteUpdaterService, GeoLiteUpdaterService>();

        // Support services (all singletons to match the updater lifetime)
        services.AddSingleton<IGeoLiteConfigProvider, GeoLiteConfigProvider>();
        services.AddSingleton<IGeoLiteAuthProvider, GeoLiteAuthProvider>();
        services.AddSingleton<IGeoLiteProgressNotifier, GeoLiteProgressNotifier>();
        services.AddSingleton<IHttpErrorMapper, HttpErrorMapper>();
        services.AddSingleton<IStreamCopier, StreamCopier>();

        services.AddSingleton<IGeoLiteScheduledUpdateRunner, GeoLiteScheduledUpdateRunner>();
        if (databaseRuntime.IsConnectionConfigured)
            services.AddHostedService<GeoLiteAutoUpdateBackgroundService>();
    }
}