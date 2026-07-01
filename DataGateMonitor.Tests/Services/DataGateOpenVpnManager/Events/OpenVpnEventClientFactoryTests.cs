using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.Events;

public class OpenVpnEventClientFactoryTests
{
    private static (OpenVpnEventClientFactory Factory, Mock<IVpnServerQueryService> ServerQuery) CreateFactory()
    {
        var serverQuery = new Mock<IVpnServerQueryService>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(serverQuery.Object);
        services.AddSingleton<IHubContext<OpenVpnEventHub>>(Mock.Of<IHubContext<OpenVpnEventHub>>());
        services.AddSingleton<IMicroserviceTokenService>(OpenVpnHubTestHelpers.CreateTokenService());
        var sp = services.BuildServiceProvider();
        return (new OpenVpnEventClientFactory(sp), serverQuery);
    }

    [Fact]
    public void Create_ReturnsCachedClient_ForSameServerId()
    {
        var (factory, _) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        var second = factory.Create(server);

        Assert.Same(first, second);
        Assert.Equal(server.ApiUrl, first.RegisteredApiUrl);
    }

    [Fact]
    public void Create_ReturnsDifferentInstances_ForDifferentServerIds()
    {
        var (factory, _) = CreateFactory();

        var client1 = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(id: 1));
        var client2 = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(id: 2));

        Assert.NotSame(client1, client2);
        Assert.Equal(2, factory.GetAllClients().Count);
    }

    [Fact]
    public void Create_ReusesInstance_WhenUrlDiffersOnlyByCaseOrTrailingSlash()
    {
        var (factory, _) = CreateFactory();
        var first = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: "https://MS.Example/api/"));
        var second = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: "https://ms.example/api"));

        Assert.Same(first, second);
    }

    [Fact]
    public void Create_RecreatesClient_WhenApiUrlChanges()
    {
        var (factory, _) = CreateFactory();
        var first = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer());
        var second = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: "https://changed.datagateapp.com/"));

        Assert.NotSame(first, second);
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
    public void Remove_StopsClientAndEvictsFromCache()
    {
        var (factory, _) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var client = factory.Create(server);
        Assert.True(factory.Remove(server.Id));
        Assert.False(factory.TryGetClientStatus(server.Id, out _));

        var recreated = factory.Create(server);
        Assert.NotSame(client, recreated);
        Assert.True(factory.TryGetClientStatus(server.Id, out _));
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenNotCached()
    {
        var (factory, _) = CreateFactory();
        Assert.False(factory.Remove(99));
    }

    [Fact]
    public void TryGetClientStatus_ReturnsStatus_AfterCreate()
    {
        var (factory, _) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        _ = factory.Create(server);

        Assert.True(factory.TryGetClientStatus(server.Id, out var status));
        Assert.NotNull(status);
        Assert.Equal(server.Id, status!.ConnectionStatus.ServerId);
    }

    [Fact]
    public void TryGetClientStatus_ReturnsFalse_WhenNotCached()
    {
        var (factory, _) = CreateFactory();
        Assert.False(factory.TryGetClientStatus(99, out var status));
        Assert.Null(status);
    }

    [Fact]
    public void GetAllClientStatuses_IncludesCreatedClients()
    {
        var (factory, _) = CreateFactory();
        _ = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(id: 1));
        _ = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(id: 2));

        var statuses = factory.GetAllClientStatuses();

        Assert.Equal(2, statuses.ConnectionStatuses.Count);
    }

    [Fact]
    public async Task TryCreateByServerIdAsync_ReturnsClient_WhenServerExists()
    {
        var (factory, serverQuery) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer(id: 5);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(server);

        var client = await factory.TryCreateByServerIdAsync(5, CancellationToken.None);

        Assert.NotNull(client);
        Assert.True(factory.TryGetClientStatus(5, out _));
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
    public void Create_Throws_ForNonOpenVpnServer()
    {
        var (factory, _) = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        server.ServerType = SharedModels.Enums.VpnServerType.Xray;

        Assert.Throws<InvalidOperationException>(() => factory.Create(server));
    }
}
