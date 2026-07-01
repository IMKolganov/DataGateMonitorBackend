using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.SharedModels.Enums;
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
    public void Create_ReusesInstance_WhenUrlDiffersOnlyByCaseOrTrailingSlash()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnProxyTrafficFlowClientFactory>();
        var first = factory.Create(new VpnServer { Id = 75, ApiUrl = "https://MS.Example/", ServerType = VpnServerType.OpenVpn });
        var second = factory.Create(new VpnServer { Id = 75, ApiUrl = "https://ms.example", ServerType = VpnServerType.OpenVpn });

        Assert.Same(first, second);
    }

    [Fact]
    public void Create_RecreatesClient_WhenApiUrlChanges()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnProxyTrafficFlowClientFactory>();

        var first = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer());
        var second = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: "https://changed.datagateapp.com/"));

        Assert.NotSame(first, second);
        Assert.Equal("https://s5.datagateapp.com/", first.RegisteredApiUrl);
        Assert.Equal("https://changed.datagateapp.com/", second.RegisteredApiUrl);
    }

    [Fact]
    public void Create_RecreatesClient_WhenSameServerObjectApiUrlIsMutated()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnProxyTrafficFlowClientFactory>();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        server.ApiUrl = "https://mutated.datagateapp.com/";
        var second = factory.Create(server);

        Assert.NotSame(first, second);
        Assert.Equal("https://s5.datagateapp.com/", first.RegisteredApiUrl);
        Assert.Equal("https://mutated.datagateapp.com/", second.RegisteredApiUrl);
    }

    [Fact]
    public void Remove_StopsClientAndEvictsFromCache()
    {
        var (sp, _) = CreateServices();
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
        server.ServerType = VpnServerType.Xray;

        Assert.Throws<InvalidOperationException>(() => factory.Create(server));
    }
}
