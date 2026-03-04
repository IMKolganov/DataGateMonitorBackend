using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Configurations;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

public class TelegramServiceConfigurationTests
{
    [Fact]
    public void ConfigureTelegramServices_Registers_TelegramAndLocalizationServices()
    {
        var services = new ServiceCollection();

        services.ConfigureTelegramServices();

        AssertRegistered(services, typeof(ITelegramUserService));
        AssertRegistered(services, typeof(ILocalizationService));
        AssertRegistered(services, typeof(IIncomingMessageLogService));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
