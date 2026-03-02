using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Configurations;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

public class HealthCheckServicesConfigurationTests
{
    [Fact]
    public void ConfigureHealthCheckServices_WhenConnectionStringProvided_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=u;Password=p"
            })
            .Build();

        var exception = Record.Exception(() => services.ConfigureHealthCheckServices(config));

        Assert.Null(exception);
        Assert.True(services.Count > 0, "Services should be registered.");
    }

    [Fact]
    public void ConfigureHealthCheckServices_WhenNoConnectionString_Throws()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var prevEnv = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE");
        try
        {
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE", null);

            Assert.Throws<InvalidOperationException>(() => services.ConfigureHealthCheckServices(config));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE", prevEnv);
        }
    }
}
