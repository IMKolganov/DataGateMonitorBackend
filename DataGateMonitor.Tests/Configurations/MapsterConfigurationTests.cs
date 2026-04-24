using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.Configurations;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class MapsterConfigurationTests
{
    [Fact]
    public void ConfigureMapster_Registers_IMapper()
    {
        var services = new ServiceCollection();

        services.ConfigureMapster();

        AssertRegistered(services, typeof(IMapper));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
