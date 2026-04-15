using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.Configurations;
using DataGateMonitor.Hubs;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.Services.Others.Notifications.CertApiClient;
using DataGateMonitor.Services.Others.Notifications.OvpnFileApi;
using DataGateMonitor.Services.Others.Notifications.ServerOpenVpnApiClient;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.Services.Others.Notifications.GeoLite;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class NotificationConfigurationTests
{
    [Fact]
    public void ConfigureNotificationServices_Registers_NotificationServices()
    {
        var services = new ServiceCollection();

        services.ConfigureNotificationServices();

        AssertRegistered(services, typeof(INotificationService));
        AssertRegistered(services, typeof(INotificationCatalog));
        AssertRegistered(services, typeof(IAppNotificationFacade));
        AssertRegistered(services, typeof(IOvpnFileNotificationService));
        AssertRegistered(services, typeof(ICertificateNotificationService));
        AssertRegistered(services, typeof(IServerOpenVpnNotificationService));
        AssertRegistered(services, typeof(IOpenVpnMicroserviceNotificationService));
        AssertRegistered(services, typeof(IGeoLiteNotificationService));
        AssertRegistered(services, typeof(IAdminNotificationHub));
        AssertRegistered(services, typeof(INotifier));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
