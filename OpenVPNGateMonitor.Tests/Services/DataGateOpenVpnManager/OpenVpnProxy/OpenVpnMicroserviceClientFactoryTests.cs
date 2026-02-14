using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using OpenVPNGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;

namespace OpenVPNGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientFactoryTests
{
    private static (OpenVpnMicroserviceClientFactory factory,
        ServiceProvider provider,
        Mock<IOpenVpnServerQueryService> serverQueryMock)
        CreateFactory()
    {
        var services = new ServiceCollection();

        // Query service
        var serverQueryMock = new Mock<IOpenVpnServerQueryService>();
        services.AddSingleton(serverQueryMock.Object);

        // Logger for OpenVpnMicroserviceClient
        services.AddLogging();

        // Hub context (not used in these tests, so minimal mock is fine)
        var hubContextMock = new Mock<IHubContext<OpenVpnFrontendHub>>();
        services.AddSingleton(hubContextMock.Object);

        // Token service (not used directly in these tests)
        var tokenServiceMock = new Mock<IMicroserviceTokenService>();
        tokenServiceMock
            .Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("dummy-token");
        services.AddSingleton(tokenServiceMock.Object);

        // Microservice notification (required by OpenVpnMicroserviceClient)
        var microserviceNotificationMock = new Mock<IOpenVpnMicroserviceNotificationService>();
        microserviceNotificationMock.Setup(n => n.NotifySendCommandFailed(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        microserviceNotificationMock.Setup(n => n.NotifyReconnectFailed(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        services.AddSingleton(microserviceNotificationMock.Object);

        var provider = services.BuildServiceProvider();
        var factory = new OpenVpnMicroserviceClientFactory(provider);

        return (factory, provider, serverQueryMock);
    }

    [Fact]
    public void Create_SameId_And_ExactUrl_ReturnsSameInstance()
    {
        var (factory, _, _) = CreateFactory();

        var server1 = new OpenVpnServer { Id = 1, ApiUrl = "http://ms.example/api" };
        var server2 = new OpenVpnServer { Id = 1, ApiUrl = "http://ms.example/api" };

        var client1 = factory.Create(server1);
        var client2 = factory.Create(server2);

        Assert.Same(client1, client2);
    }

    [Fact]
    public void Create_SameId_UrlDiffersOnlyByCaseOrTrailingSlash_ReusesInstance()
    {
        var (factory, _, _) = CreateFactory();

        var server1 = new OpenVpnServer { Id = 1, ApiUrl = "http://MS.Example/api/" };
        var server2 = new OpenVpnServer { Id = 1, ApiUrl = "http://ms.example/api" };

        var client1 = factory.Create(server1);
        var client2 = factory.Create(server2);

        Assert.Same(client1, client2);
    }

    [Fact]
    public void Create_SameId_DifferentUrl_CreatesNewInstance()
    {
        var (factory, _, _) = CreateFactory();

        var server1 = new OpenVpnServer { Id = 1, ApiUrl = "http://ms.example/a" };
        var server2 = new OpenVpnServer { Id = 1, ApiUrl = "http://ms.example/b" };

        var client1 = factory.Create(server1);
        var client2 = factory.Create(server2);

        Assert.NotSame(client1, client2);
    }

    [Fact]
    public async Task TryCreateByServerIdAsync_ReturnsClient_WhenServerExists()
    {
        var (factory, _, serverQueryMock) = CreateFactory();

        var server = new OpenVpnServer
        {
            Id = 42,
            ApiUrl = "http://ms.example/openvpn"
        };

        serverQueryMock
            .Setup(q => q.GetById(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        var client = await factory.TryCreateByServerIdAsync(42, CancellationToken.None);

        Assert.NotNull(client);

        // Cache should return the same instance for the same server id
        var client2 = await factory.TryCreateByServerIdAsync(42, CancellationToken.None);
        Assert.Same(client, client2);

        serverQueryMock.Verify(q => q.GetById(42, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TryCreateByServerIdAsync_Throws_WhenServerNotFound()
    {
        var (factory, _, serverQueryMock) = CreateFactory();

        serverQueryMock
            .Setup(q => q.GetById(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenVpnServer?)null);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => factory.TryCreateByServerIdAsync(99, CancellationToken.None));

        Assert.Contains("OpenVPN server not found with id 99", ex.Message);
    }
}
