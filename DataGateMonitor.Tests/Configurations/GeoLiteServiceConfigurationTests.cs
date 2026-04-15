using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.Configurations;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.GeoLite.Interfaces;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class GeoLiteServiceConfigurationTests
{
    [Fact]
    public void ConfigureGeoLiteServices_Registers_GeoLiteServices()
    {
        var services = new ServiceCollection();

        services.ConfigureGeoLiteServices();

        AssertRegistered(services, typeof(IGeoLiteQueryService));
        AssertRegistered(services, typeof(IGeoLiteUpdaterService));
        AssertRegistered(services, typeof(IGeoLiteConfigProvider));
        AssertRegistered(services, typeof(IGeoLiteScheduledUpdateRunner));
        AssertRegistered(services, typeof(GeoLiteDatabaseFactory));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
