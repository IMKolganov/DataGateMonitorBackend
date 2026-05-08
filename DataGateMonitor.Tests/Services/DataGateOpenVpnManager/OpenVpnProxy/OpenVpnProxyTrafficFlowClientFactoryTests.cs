using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnProxyTrafficFlowClientFactoryTests
{
    private static OpenVpnProxyTrafficFlowClientFactory CreateFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHubContext<OpenVpnProxyTrafficFlowHub>>());

        var tokenService = new Mock<IMicroserviceTokenService>();
        tokenService
            .Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("token");
        services.AddSingleton(tokenService.Object);

        var provider = services.BuildServiceProvider();
        return new OpenVpnProxyTrafficFlowClientFactory(provider);
    }

    [Fact]
    public void Create_SameServerId_ReturnsCachedInstance()
    {
        var factory = CreateFactory();
        var server = new VpnServer { Id = 1, ApiUrl = "https://node/a", ServerType = VpnServerType.OpenVpn };

        var client1 = factory.Create(server);
        var client2 = factory.Create(new VpnServer { Id = 1, ApiUrl = "https://node/b", ServerType = VpnServerType.OpenVpn });

        Assert.Same(client1, client2);
    }

    [Fact]
    public void Create_XrayServer_Throws()
    {
        var factory = CreateFactory();
        var xrayServer = new VpnServer { Id = 7, ApiUrl = "https://xray", ServerType = VpnServerType.Xray };

        var ex = Assert.Throws<InvalidOperationException>(() => factory.Create(xrayServer));

        Assert.Contains("only for OpenVPN servers", ex.Message);
    }

    [Fact]
    public void GetAllClients_ReturnsAllCachedClients()
    {
        var factory = CreateFactory();
        factory.Create(new VpnServer { Id = 1, ApiUrl = "https://node-1", ServerType = VpnServerType.OpenVpn });
        factory.Create(new VpnServer { Id = 2, ApiUrl = "https://node-2", ServerType = VpnServerType.OpenVpn });

        var all = factory.GetAllClients();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Remove_WhenNotFound_ReturnsFalse()
    {
        var factory = CreateFactory();

        var removed = factory.Remove(999);

        Assert.False(removed);
    }

    [Fact]
    public void Remove_WhenFound_ReturnsTrue_AndRemovesFromCache()
    {
        var factory = CreateFactory();
        factory.Create(new VpnServer { Id = 3, ApiUrl = "https://node-3", ServerType = VpnServerType.OpenVpn });

        var removed = factory.Remove(3);

        Assert.True(removed);
        Assert.Empty(factory.GetAllClients());
    }
}
