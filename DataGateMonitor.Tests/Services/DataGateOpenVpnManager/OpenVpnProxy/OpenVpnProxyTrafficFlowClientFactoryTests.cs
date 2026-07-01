using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnProxyTrafficFlowClientFactoryTests
{
    private static (IServiceProvider Sp, FakeHubConnectionFactory HubFactory) CreateServices()
    {
        var hubFactory = new FakeHubConnectionFactory();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHubContext<OpenVpnProxyTrafficFlowHub>>(Mock.Of<IHubContext<OpenVpnProxyTrafficFlowHub>>());
        services.AddSingleton<IMicroserviceTokenService>(OpenVpnHubTestHelpers.CreateTokenService());
        services.AddSingleton<IHubConnectionFactory>(hubFactory);
        services.AddSingleton<OpenVpnProxyTrafficFlowClientFactory>();
        return (services.BuildServiceProvider(), hubFactory);
    }

    [Fact]
    public void Create_ReturnsCachedClient_ForSameServerId()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnProxyTrafficFlowClientFactory>();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        var second = factory.Create(server);

        Assert.Same(first, second);
    }

    [Fact]
    public void Remove_StopsClientAndEvictsFromCache()
    {
        var (sp, hubFactory) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnProxyTrafficFlowClientFactory>();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        Assert.True(factory.Remove(server.Id));

        var recreated = factory.Create(server);
        Assert.NotSame(first, recreated);
    }

    [Fact]
    public void Create_Throws_ForNonOpenVpnServer()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnProxyTrafficFlowClientFactory>();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        server.ServerType = SharedModels.Enums.VpnServerType.Xray;

        Assert.Throws<InvalidOperationException>(() => factory.Create(server));
    }
}
