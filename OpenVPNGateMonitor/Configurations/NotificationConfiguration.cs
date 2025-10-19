using OpenVPNGateMonitor.Services.Others.Notifications;
using OpenVPNGateMonitor.Services.Others.Notifications.OvpnFileApi;

namespace OpenVPNGateMonitor.Configurations;

public static class NotificationConfiguration
{
    public static void ConfigureNotificationServices(this IServiceCollection services)
    {
        services.AddSingleton<INotificationCatalog, NotificationCatalog>();
        services.AddScoped<IAppNotificationFacade, AppNotificationFacade>();
        
        services.AddScoped<IOvpnFileNotificationService, OvpnFileNotificationService>();
    }
}