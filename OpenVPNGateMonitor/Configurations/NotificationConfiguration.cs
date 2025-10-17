using OpenVPNGateMonitor.Services.Others.Notifications;

namespace OpenVPNGateMonitor.Configurations;

public static class NotificationConfiguration
{
    public static void ConfigureNotificationServices(this IServiceCollection services)
    {
        services.AddSingleton<INotificationCatalog, NotificationCatalog>();
        services.AddScoped<IAppNotificationFacade, AppNotificationFacade>();
    }
}