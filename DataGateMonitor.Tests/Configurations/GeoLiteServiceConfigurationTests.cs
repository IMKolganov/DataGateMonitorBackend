using Microsoft.Extensions.Configuration;
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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=u;Password=p"
            })
            .Build();
        var databaseRuntime = DatabaseRuntimeOptions.FromConfiguration(config);

        services.ConfigureGeoLiteServices(databaseRuntime);

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
