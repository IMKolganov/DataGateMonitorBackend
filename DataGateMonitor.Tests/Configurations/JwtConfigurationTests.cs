using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.Configurations;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class JwtConfigurationTests
{
    [Fact]
    public void ConfigureJwt_Registers_AuthenticationAndMicroserviceTokenService()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = "min-32-char-secret-for-testing!!!!" })
            .Build();

        services.ConfigureJwt(config);

        AssertRegistered(services, typeof(IMicroserviceTokenService));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService) ||
            d.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider));
    }

    [Fact]
    public void ConfigureJwt_WhenJwtSecretMissing_Throws()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => services.ConfigureJwt(config));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
