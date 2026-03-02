using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Configurations;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Services.Others.Notifications;
using OpenVPNGateMonitor.Services.Others.Notifications.CertApiClient;
using OpenVPNGateMonitor.Services.Others.Notifications.OvpnFileApi;
using OpenVPNGateMonitor.Services.Others.Notifications.ServerOpenVpnApiClient;
using OpenVPNGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

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
        AssertRegistered(services, typeof(IAdminNotificationHub));
        AssertRegistered(services, typeof(INotifier));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
