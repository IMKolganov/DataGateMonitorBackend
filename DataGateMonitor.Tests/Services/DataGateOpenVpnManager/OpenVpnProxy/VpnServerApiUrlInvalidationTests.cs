using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

/// <summary>
/// Cross-cutting contract: when VpnServer.ApiUrl changes, factories must evict/dispose stale clients
/// and clients must recreate SignalR connections. Equivalent URLs (case/trailing slash) must not trigger that.
/// </summary>
public class VpnServerApiUrlInvalidationTests
{
    [Fact]
    public async Task MicroserviceFactory_DisposesActiveHub_WhenApiUrlChanges()
    {
        var hubFactory = new FakeHubConnectionFactory();
        var factory = CreateMicroserviceFactory(hubFactory);
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var firstClient = factory.Create(server);
        await firstClient.SendCommandAsync("ping", CancellationToken.None);
        var firstProxy = hubFactory.Created[0];

        var secondClient = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: "https://changed.datagateapp.com/"));

        Assert.True(firstProxy.Disposed);
        Assert.NotSame(firstClient, secondClient);
        await secondClient.SendCommandAsync("status", CancellationToken.None);
        Assert.Equal(2, hubFactory.Created.Count);
    }

    [Fact]
    public async Task EventFactory_DisposesActiveHub_WhenApiUrlChanges()
    {
        var hubFactory = new FakeHubConnectionFactory();
        var factory = CreateEventFactory(hubFactory);
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var firstClient = factory.Create(server);
        await firstClient.StartListeningAsync(CancellationToken.None);
        var firstProxy = hubFactory.Created[0];

        var secondClient = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: "https://changed.datagateapp.com/"));

        Assert.True(firstProxy.Disposed);
        Assert.NotSame(firstClient, secondClient);
        await secondClient.StartListeningAsync(CancellationToken.None);
        Assert.Equal(2, hubFactory.Created.Count);
    }

    [Fact]
    public async Task TrafficFlowFactory_DisposesActiveHub_WhenApiUrlChanges()
    {
        var hubFactory = new FakeHubConnectionFactory();
        var factory = CreateTrafficFlowFactory(hubFactory);
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var firstClient = factory.Create(server);
        await firstClient.StartListeningAsync(CancellationToken.None);
        var firstProxy = hubFactory.Created[0];

        var secondClient = factory.Create(OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: "https://changed.datagateapp.com/"));

        Assert.True(firstProxy.Disposed);
        Assert.NotSame(firstClient, secondClient);
        await secondClient.StartListeningAsync(CancellationToken.None);
        Assert.Equal(2, hubFactory.Created.Count);
    }

    [Fact]
    public async Task EventFactory_TryCreateByServerId_RecreatesClient_WhenDbUrlChanges()
    {
        var hubFactory = new FakeHubConnectionFactory();
        var (factory, serverQuery) = CreateEventFactoryWithQuery(hubFactory);
        var server = OpenVpnHubTestHelpers.OpenVpnServer(id: 5, apiUrl: "https://old.example.com/");
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(() => server);

        var first = await factory.TryCreateByServerIdAsync(5, CancellationToken.None);
        await first!.StartListeningAsync(CancellationToken.None);
        var firstProxy = hubFactory.Created[0];

        server.ApiUrl = "https://new.example.com/";
        var second = await factory.TryCreateByServerIdAsync(5, CancellationToken.None);

        Assert.NotSame(first, second);
        Assert.True(firstProxy.Disposed);
        Assert.Equal("https://new.example.com/", second!.RegisteredApiUrl);
    }

    [Fact]
    public async Task MicroserviceFactory_TryCreateByServerId_RecreatesClient_WhenDbUrlChanges()
    {
        var hubFactory = new FakeHubConnectionFactory();
        var (serverQuery, factory) = CreateMicroserviceFactoryWithQuery(hubFactory);
        var server = OpenVpnHubTestHelpers.OpenVpnServer(id: 42, apiUrl: "https://old.example.com/");
        serverQuery.Setup(q => q.GetById(42, It.IsAny<CancellationToken>())).ReturnsAsync(() => server);

        var first = await factory.TryCreateByServerIdAsync(42, CancellationToken.None);
        await first!.SendCommandAsync("ping", CancellationToken.None);
        var firstProxy = hubFactory.Created[0];

        server.ApiUrl = "https://new.example.com/";
        var second = await factory.TryCreateByServerIdAsync(42, CancellationToken.None);

        Assert.NotSame(first, second);
        Assert.True(firstProxy.Disposed);
        Assert.Equal("https://new.example.com/", second!.RegisteredApiUrl);
    }

    [Theory]
    [InlineData("https://MS.Example/", "https://ms.example")]
    [InlineData("https://host.example.com/api/", "https://host.example.com/api")]
    public async Task MicroserviceClient_DoesNotRecreateHub_WhenUrlDiffersOnlyByNormalization(
        string initialUrl,
        string mutatedUrl)
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: initialUrl);
        var hubFactory = new FakeHubConnectionFactory();
        var client = CreateMicroserviceClient(server, hubFactory);

        await client.SendCommandAsync("ping", CancellationToken.None);
        server.ApiUrl = mutatedUrl;
        await client.SendCommandAsync("status", CancellationToken.None);

        Assert.Single(hubFactory.Created);
    }

    [Theory]
    [InlineData("https://MS.Example/", "https://ms.example")]
    [InlineData("https://host.example.com/api/", "https://host.example.com/api")]
    public async Task EventClient_DoesNotRecreateHub_WhenUrlDiffersOnlyByNormalization(
        string initialUrl,
        string mutatedUrl)
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: initialUrl);
        var hubFactory = new FakeHubConnectionFactory();
        var client = CreateEventClient(server, hubFactory);

        await client.StartListeningAsync(CancellationToken.None);
        server.ApiUrl = mutatedUrl;
        await client.StartListeningAsync(CancellationToken.None);

        Assert.Single(hubFactory.Created);
    }

    [Theory]
    [InlineData("https://MS.Example/", "https://ms.example")]
    [InlineData("https://host.example.com/api/", "https://host.example.com/api")]
    public async Task TrafficFlowClient_DoesNotRecreateHub_WhenUrlDiffersOnlyByNormalization(
        string initialUrl,
        string mutatedUrl)
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer(apiUrl: initialUrl);
        var hubFactory = new FakeHubConnectionFactory();
        var client = CreateTrafficFlowClient(server, hubFactory);

        await client.StartListeningAsync(CancellationToken.None);
        server.ApiUrl = mutatedUrl;
        await client.StartListeningAsync(CancellationToken.None);

        Assert.Single(hubFactory.Created);
    }

    private static OpenVpnMicroserviceClientFactory CreateMicroserviceFactory(FakeHubConnectionFactory hubFactory)
    {
        var (_, factory) = CreateMicroserviceFactoryWithQuery(hubFactory);
        return factory;
    }

    private static (Mock<IVpnServerQueryService> ServerQuery, OpenVpnMicroserviceClientFactory Factory)
        CreateMicroserviceFactoryWithQuery(FakeHubConnectionFactory hubFactory)
    {
        var serverQuery = new Mock<IVpnServerQueryService>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(serverQuery.Object);
        services.AddSingleton<IHubContext<OpenVpnFrontendHub>>(Mock.Of<IHubContext<OpenVpnFrontendHub>>());
        services.AddSingleton<IMicroserviceTokenService>(OpenVpnHubTestHelpers.CreateTokenService());
        services.AddSingleton<IHubConnectionFactory>(hubFactory);
        services.AddSingleton(Mock.Of<IOpenVpnMicroserviceNotificationService>());
        services.AddSingleton<OpenVpnMicroserviceClientFactory>();
        var sp = services.BuildServiceProvider();
        return (serverQuery, sp.GetRequiredService<OpenVpnMicroserviceClientFactory>());
    }

    private static OpenVpnEventClientFactory CreateEventFactory(FakeHubConnectionFactory hubFactory)
    {
        var (factory, _) = CreateEventFactoryWithQuery(hubFactory);
        return factory;
    }

    private static (OpenVpnEventClientFactory Factory, Mock<IVpnServerQueryService> ServerQuery)
        CreateEventFactoryWithQuery(FakeHubConnectionFactory hubFactory)
    {
        var serverQuery = new Mock<IVpnServerQueryService>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(serverQuery.Object);
        services.AddSingleton<IHubContext<OpenVpnEventHub>>(Mock.Of<IHubContext<OpenVpnEventHub>>());
        services.AddSingleton<IMicroserviceTokenService>(OpenVpnHubTestHelpers.CreateTokenService());
        services.AddSingleton<IEventHubConnectionFactory>(hubFactory);
        var sp = services.BuildServiceProvider();
        return (new OpenVpnEventClientFactory(sp), serverQuery);
    }

    private static OpenVpnProxyTrafficFlowClientFactory CreateTrafficFlowFactory(FakeHubConnectionFactory hubFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHubContext<OpenVpnProxyTrafficFlowHub>>(Mock.Of<IHubContext<OpenVpnProxyTrafficFlowHub>>());
        services.AddSingleton<IMicroserviceTokenService>(OpenVpnHubTestHelpers.CreateTokenService());
        services.AddSingleton<IHubConnectionFactory>(hubFactory);
        services.AddSingleton<OpenVpnProxyTrafficFlowClientFactory>();
        return services.BuildServiceProvider().GetRequiredService<OpenVpnProxyTrafficFlowClientFactory>();
    }

    private static OpenVpnMicroserviceClient CreateMicroserviceClient(VpnServer server, FakeHubConnectionFactory hubFactory)
    {
        var (hub, _) = OpenVpnHubTestHelpers.CreateFrontendHubMock(server.Id);
        return new OpenVpnMicroserviceClient(
            server,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenVpnMicroserviceClient>.Instance,
            hub.Object,
            OpenVpnHubTestHelpers.CreateTokenService(),
            OpenVpnHubTestHelpers.CreateScopeFactory(),
            hubFactory);
    }

    private static OpenVpnEventClient CreateEventClient(VpnServer server, FakeHubConnectionFactory hubFactory) =>
        new(
            server,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenVpnEventClient>.Instance,
            Mock.Of<IHubContext<OpenVpnEventHub>>(),
            OpenVpnHubTestHelpers.CreateTokenService(),
            Mock.Of<IServiceScopeFactory>(),
            hubFactory);

    private static OpenVpnProxyTrafficFlowClient CreateTrafficFlowClient(VpnServer server, FakeHubConnectionFactory hubFactory) =>
        new(
            server,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenVpnProxyTrafficFlowClient>.Instance,
            OpenVpnHubTestHelpers.CreateTrafficFlowHubMock(server.Id).Hub.Object,
            OpenVpnHubTestHelpers.CreateTokenService(),
            hubFactory);
}
