using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientFactoryTests
{
    private static (OpenVpnMicroserviceClientFactory Factory, Mock<IVpnServerQueryService> ServerQuery) CreateFactory(
        FakeHubConnectionFactory? hubFactory = null)
    {
        hubFactory ??= new FakeHubConnectionFactory();
        var serverQueryMock = new Mock<IVpnServerQueryService>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(serverQueryMock.Object);
        services.AddSingleton<IHubContext<OpenVpnFrontendHub>>(Mock.Of<IHubContext<OpenVpnFrontendHub>>());
        services.AddSingleton<IMicroserviceTokenService>(OpenVpnHubTestHelpers.CreateTokenService());
        services.AddSingleton<IHubConnectionFactory>(hubFactory);
        services.AddSingleton(Mock.Of<IOpenVpnMicroserviceNotificationService>());
        services.AddSingleton<OpenVpnMicroserviceClientFactory>();
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<OpenVpnMicroserviceClientFactory>(), serverQueryMock);
    }

    [Fact]
    public void Create_ReturnsCachedClient_ForSameServerId()
    {
        var (factory, _) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        var second = factory.Create(server);

        Assert.Same(first, second);
    }

    [Fact]
    public void Create_ReusesInstance_WhenUrlDiffersOnlyByCaseOrTrailingSlash()
    {
        var (factory, _) = CreateFactory();
        var first = factory.Create(new VpnServer { Id = 1, ApiUrl = "https://MS.Example/api/", ServerType = VpnServerType.OpenVpn });
        var second = factory.Create(new VpnServer { Id = 1, ApiUrl = "https://ms.example/api", ServerType = VpnServerType.OpenVpn });

        Assert.Same(first, second);
    }

    [Fact]
    public void Create_RecreatesClient_WhenApiUrlChanges()
    {
        var (factory, _) = CreateFactory();
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
        var (factory, _) = CreateFactory();
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
        var (factory, _) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        factory.Invalidate(server.Id);
        var second = factory.Create(server);

        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task TryCreateByServerIdAsync_ReturnsClient_WhenServerExists()
    {
        var (factory, serverQuery) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer(id: 42);
        serverQuery.Setup(q => q.GetById(42, It.IsAny<CancellationToken>())).ReturnsAsync(server);

        var client = await factory.TryCreateByServerIdAsync(42, CancellationToken.None);
        var cached = await factory.TryCreateByServerIdAsync(42, CancellationToken.None);

        Assert.NotNull(client);
        Assert.Same(client, cached);
        serverQuery.Verify(q => q.GetById(42, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TryCreateByServerIdAsync_ReturnsNull_WhenServerNotFound()
    {
        var (factory, serverQuery) = CreateFactory();
        serverQuery.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((VpnServer?)null);

        var client = await factory.TryCreateByServerIdAsync(99, CancellationToken.None);

        Assert.Null(client);
    }

    [Fact]
    public async Task TryCreateByServerIdAsync_ReturnsNull_WhenServerIsXray()
    {
        var (factory, serverQuery) = CreateFactory();
        serverQuery
            .Setup(q => q.GetById(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnServer { Id = 7, ApiUrl = "https://xray/", ServerType = VpnServerType.Xray });

        var client = await factory.TryCreateByServerIdAsync(7, CancellationToken.None);

        Assert.Null(client);
    }

    [Fact]
    public void Create_Throws_ForNonOpenVpnServer()
    {
        var (factory, _) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        server.ServerType = VpnServerType.Xray;

        Assert.Throws<InvalidOperationException>(() => factory.Create(server));
    }
}
