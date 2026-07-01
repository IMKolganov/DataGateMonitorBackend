using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Helpers;

internal static class OpenVpnHubTestHelpers
{
    public static VpnServer OpenVpnServer(int id = 75, string apiUrl = "https://s5.datagateapp.com/") =>
        new()
        {
            Id = id,
            ServerName = "Test Norway",
            ApiUrl = apiUrl,
            ServerType = SharedModels.Enums.VpnServerType.OpenVpn,
        };

    public static (Mock<IHubContext<OpenVpnFrontendHub>> Hub, Mock<IClientProxy> Client) CreateFrontendHubMock(int serverId)
    {
        var hubClients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        hubClients.Setup(h => h.Group(serverId.ToString())).Returns(clientProxy.Object);
        clientProxy
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubContext = new Mock<IHubContext<OpenVpnFrontendHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);
        return (hubContext, clientProxy);
    }

    public static (Mock<IHubContext<OpenVpnProxyTrafficFlowHub>> Hub, Mock<IClientProxy> Client) CreateTrafficFlowHubMock(int serverId)
    {
        var hubClients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        hubClients.Setup(h => h.Group(serverId.ToString())).Returns(clientProxy.Object);
        clientProxy
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubContext = new Mock<IHubContext<OpenVpnProxyTrafficFlowHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);
        return (hubContext, clientProxy);
    }

    public static IServiceScopeFactory CreateScopeFactory(
        IOpenVpnMicroserviceNotificationService? notifications = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(notifications ?? new Mock<IOpenVpnMicroserviceNotificationService>().Object);
        var root = services.BuildServiceProvider();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(() => root.CreateScope());
        return scopeFactory.Object;
    }

    public static IMicroserviceTokenService CreateTokenService() =>
        Mock.Of<IMicroserviceTokenService>(x =>
            x.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            == "test-jwt");
}
