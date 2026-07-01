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

public class OpenVpnMicroserviceClientFactoryTests
{
    private static (IServiceProvider Sp, FakeHubConnectionFactory HubFactory) CreateServices()
    {
        var hubFactory = new FakeHubConnectionFactory();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHubContext<OpenVpnFrontendHub>>(Mock.Of<IHubContext<OpenVpnFrontendHub>>());
        services.AddSingleton<IMicroserviceTokenService>(OpenVpnHubTestHelpers.CreateTokenService());
        services.AddSingleton<IHubConnectionFactory>(hubFactory);
        services.AddSingleton<OpenVpnMicroserviceClientFactory>();
        var sp = services.BuildServiceProvider();
        return (sp, hubFactory);
    }

  [Fact]
    public void Create_ReturnsCachedClient_ForSameServerId()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnMicroserviceClientFactory>();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        var second = factory.Create(server);

        Assert.Same(first, second);
    }

    [Fact]
    public void Create_RecreatesClient_WhenApiUrlChanges()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnMicroserviceClientFactory>();
        var firstServer = OpenVpnHubTestHelpers.OpenVpnServer();
        var changedServer = OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: "https://changed.datagateapp.com/");

        var first = factory.Create(firstServer);
        var second = factory.Create(changedServer);

        Assert.NotSame(first, second);
        Assert.Equal("https://s5.datagateapp.com/", first.RegisteredApiUrl);
        Assert.Equal("https://changed.datagateapp.com/", second.RegisteredApiUrl);
    }

    [Fact]
    public void Create_RecreatesClient_WhenSameServerObjectApiUrlIsMutated()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnMicroserviceClientFactory>();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        server.ApiUrl = "https://mutated.datagateapp.com/";
        var second = factory.Create(server);

        Assert.NotSame(first, second);
        Assert.Equal("https://s5.datagateapp.com/", first.RegisteredApiUrl);
        Assert.Equal("https://mutated.datagateapp.com/", second.RegisteredApiUrl);
    }

    [Fact]
    public void Invalidate_RemovesClientFromCache()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnMicroserviceClientFactory>();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        factory.Invalidate(server.Id);
        var second = factory.Create(server);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Create_Throws_ForNonOpenVpnServer()
    {
        var (sp, _) = CreateServices();
        var factory = sp.GetRequiredService<OpenVpnMicroserviceClientFactory>();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        server.ServerType = SharedModels.Enums.VpnServerType.Xray;

        Assert.Throws<InvalidOperationException>(() => factory.Create(server));
    }
}
