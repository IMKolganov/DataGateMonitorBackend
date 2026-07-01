using DataGateMonitor.Hubs;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.Events;

public class OpenVpnEventClientConnectionTests
{
    [Fact]
    public async Task StartListeningAsync_UsesInjectedHubFactory_AndStartsOnce()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy();
        var eventHubFactory = new SingleProxyEventHubConnectionFactory(proxy);

        var services = new ServiceCollection();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(Mock.Of<IServiceScope>());

        var client = new OpenVpnEventClient(
            server,
            NullLogger<OpenVpnEventClient>.Instance,
            Mock.Of<IHubContext<OpenVpnEventHub>>(),
            OpenVpnHubTestHelpers.CreateTokenService(),
            scopeFactory.Object,
            eventHubFactory);

        await client.StartListeningAsync(CancellationToken.None);
        await client.StartListeningAsync(CancellationToken.None);

        Assert.Equal(1, proxy.StartCallCount);
        Assert.Equal(HubConnectionState.Connected, proxy.State);
        Assert.Contains("openvpn-event", client.GetStatus().ConnectionStatus.Url);
    }

    [Fact]
    public async Task StartListeningAsync_WhenReconnecting_WaitsWithoutSecondStart()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy();
        var client = new OpenVpnEventClient(
            server,
            NullLogger<OpenVpnEventClient>.Instance,
            Mock.Of<IHubContext<OpenVpnEventHub>>(),
            OpenVpnHubTestHelpers.CreateTokenService(),
            Mock.Of<IServiceScopeFactory>(),
            new SingleProxyEventHubConnectionFactory(proxy));

        await client.StartListeningAsync(CancellationToken.None);
        proxy.State = HubConnectionState.Reconnecting;
        _ = Task.Run(() => proxy.SimulateReconnectingThenConnectedAsync(TimeSpan.FromMilliseconds(50)));

        await client.StartListeningAsync(CancellationToken.None);

        Assert.Equal(1, proxy.StartCallCount);
    }

    [Fact]
    public async Task StartListeningAsync_RecreatesConnection_WhenBoundServerApiUrlChanges()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var client = new OpenVpnEventClient(
            server,
            NullLogger<OpenVpnEventClient>.Instance,
            Mock.Of<IHubContext<OpenVpnEventHub>>(),
            OpenVpnHubTestHelpers.CreateTokenService(),
            Mock.Of<IServiceScopeFactory>(),
            hubFactory);

        await client.StartListeningAsync(CancellationToken.None);
        Assert.Single(hubFactory.Created);

        server.ApiUrl = "https://mutated.datagateapp.com/";
        await client.StartListeningAsync(CancellationToken.None);

        Assert.Equal(2, hubFactory.Created.Count);
        Assert.True(hubFactory.Created[0].Disposed);
    }
}
