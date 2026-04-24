using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.Configurations;
using DataGateMonitor.Services.Api.Auth;
using DataGateMonitor.Services.Api.Auth.ForgotPassword;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Api.Auth.TelegramLogin;
using DataGateMonitor.Services.Api.Auth.Users;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class AuthServiceConfigurationTests
{
    private static IConfiguration CreateMinimalConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleAuth:ClientId"] = "",
                ["GoogleAuth:ClientSecret"] = ""
            })
            .Build();
    }

    [Fact]
    public void ConfigureAuthServices_Registers_KeyAuthServices()
    {
        var services = new ServiceCollection();
        var config = CreateMinimalConfig();

        services.ConfigureAuthServices(config);

        AssertRegistered(services, typeof(ICurrentUserService));
        AssertRegistered(services, typeof(IUserLoginService));
        AssertRegistered(services, typeof(ITokenService));
        AssertRegistered(services, typeof(IUserAccountService));
        AssertRegistered(services, typeof(IUserRoleService));
        AssertRegistered(services, typeof(IUserRegistrationService));
        AssertRegistered(services, typeof(IGoogleAuthCodeExchangeService));
        AssertRegistered(services, typeof(IApplicationService));
        AssertRegistered(services, typeof(IGoogleTokenValidator));
        AssertRegistered(services, typeof(IAdminForgotPasswordService));
        AssertRegistered(services, typeof(ITelegramLoginCodeService));
        AssertRegistered(services, typeof(IVpnServerAccessQueryService));
        AssertRegistered(services, typeof(IDeviceService));
        AssertRegistered(services, typeof(IUserQuotaPlanService));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
