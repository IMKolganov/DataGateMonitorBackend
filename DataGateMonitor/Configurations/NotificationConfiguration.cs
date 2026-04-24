using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.Services.Others.Notifications.CertApiClient;
using DataGateMonitor.Services.Others.Notifications.OvpnFileApi;
using DataGateMonitor.Services.Others.Notifications.ServerOpenVpnApiClient;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.Services.Others.Notifications.GeoLite;
using DataGateMonitor.Hubs;

namespace DataGateMonitor.Configurations;

public static class NotificationConfiguration
{
    public static void ConfigureNotificationServices(this IServiceCollection services)
    {
        // Core service that depends on Dictionary<string, INotifier>
        services.AddScoped<INotificationService, NotificationService>();

        // Catalog/facades
        services.AddSingleton<INotificationCatalog, NotificationCatalog>();
        services.AddScoped<IAppNotificationFacade, AppNotificationFacade>();

        // Area-specific adapters
        services.AddScoped<IVpnProfileNotificationPreferenceService, VpnProfileNotificationPreferenceService>();
        services.AddScoped<IOvpnFileNotificationService, OvpnFileNotificationService>();
        services.AddScoped<ICertificateNotificationService, CertificateNotificationService>();
        services.AddScoped<IServerOpenVpnNotificationService, ServerOpenVpnNotificationService>();
        services.AddScoped<IOpenVpnMicroserviceNotificationService, OpenVpnMicroserviceNotificationService>();
        services.AddScoped<IGeoLiteNotificationService, GeoLiteNotificationService>();

        // Admin hub adapter for WebNotifier
        services.AddSingleton<IAdminNotificationHub, AdminNotificationHubService>(); // <— ADD THIS

        // Channel notifiers
        services.AddScoped<INotifier, WebNotifier>();
        // services.AddScoped<INotifier, TelegramNotifier>(); // Uncomment only if implemented

        // Provide Dictionary<string, INotifier> for NotificationService
        services.AddScoped<Dictionary<string, INotifier>>(sp =>
        {
            var notifiers = sp.GetServices<INotifier>();
            var dict = new Dictionary<string, INotifier>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in notifiers)
            {
                var key = (n.Channel ?? n.GetType().Name).Trim();
                dict[key] = n;
            }
            return dict;
        });

        services.AddScoped<IReadOnlyDictionary<string, INotifier>>(sp =>
            sp.GetRequiredService<Dictionary<string, INotifier>>());
    }
}