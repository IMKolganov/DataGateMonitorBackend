using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Configurations;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using OpenVPNGateMonitor.Services.QuotaPlans;
using OpenVPNGateMonitor.Services.Tags;
using OpenVPNGateMonitor.Services.UserRoles;
using OpenVPNGateMonitor.Services.Users;
using OpenVPNGateMonitor.Services.Users.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

public class ServiceConfigurationTests
{
    private static IConfiguration CreateMinimalConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    [Fact]
    public void ConfigureServices_Registers_KeyApplicationServices()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfig();

        services.ConfigureServices(config);

        AssertRegistered(services, typeof(IOpenVpnClientService));
        AssertRegistered(services, typeof(IOpenVpnServerService));
        AssertRegistered(services, typeof(IVpnDataService));
        AssertRegistered(services, typeof(IVpnServerStatisticsService));
        AssertRegistered(services, typeof(IOpenVpnBackgroundService));
        AssertRegistered(services, typeof(IOpenVpnServerOvpnFileConfigService));
        AssertRegistered(services, typeof(IExternalIpAddressService));
        AssertRegistered(services, typeof(IUserService));
        AssertRegistered(services, typeof(IQuotaPlanService));
        AssertRegistered(services, typeof(IUserRoleManagementService));
        AssertRegistered(services, typeof(ITagService));
        AssertRegistered(services, typeof(IMicroserviceInfoService));
        AssertRegistered(services, typeof(IOpenVpnServerConflogService));
        AssertRegistered(services, typeof(IOpenVpnMicroserviceClientFactory));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
