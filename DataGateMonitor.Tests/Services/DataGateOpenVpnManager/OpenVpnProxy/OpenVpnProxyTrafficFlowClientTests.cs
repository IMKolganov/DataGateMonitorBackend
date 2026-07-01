using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnProxyTrafficFlowClientTests
{
    [Fact]
    public async Task StartListeningAsync_WhenDisconnected_StartsConnection()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var sut = new OpenVpnProxyTrafficFlowClient(
            server,
            NullLogger<OpenVpnProxyTrafficFlowClient>.Instance,
            OpenVpnHubTestHelpers.CreateTrafficFlowHubMock(server.Id).Hub.Object,
            OpenVpnHubTestHelpers.CreateTokenService(),
            hubFactory);

        await sut.StartListeningAsync(CancellationToken.None);

        Assert.Equal(1, hubFactory.LastCreated!.StartCallCount);
    }

    [Fact]
    public async Task StartListeningAsync_WhenReconnecting_DoesNotCallStart()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var sut = new OpenVpnProxyTrafficFlowClient(
            server,
            NullLogger<OpenVpnProxyTrafficFlowClient>.Instance,
            OpenVpnHubTestHelpers.CreateTrafficFlowHubMock(server.Id).Hub.Object,
            OpenVpnHubTestHelpers.CreateTokenService(),
            hubFactory);

        await sut.StartListeningAsync(CancellationToken.None);
        var proxy = hubFactory.LastCreated!;
        proxy.State = HubConnectionState.Reconnecting;
        _ = Task.Run(() => proxy.SimulateReconnectingThenConnectedAsync(TimeSpan.FromMilliseconds(50)));

        await sut.StartListeningAsync(CancellationToken.None);

        Assert.Equal(1, proxy.StartCallCount);
    }

    [Fact]
    public async Task StartListeningAsync_SecondCall_DoesNotStartAgain()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var sut = new OpenVpnProxyTrafficFlowClient(
            server,
            NullLogger<OpenVpnProxyTrafficFlowClient>.Instance,
            OpenVpnHubTestHelpers.CreateTrafficFlowHubMock(server.Id).Hub.Object,
            OpenVpnHubTestHelpers.CreateTokenService(),
            hubFactory);

        await sut.StartListeningAsync(CancellationToken.None);
        await sut.StartListeningAsync(CancellationToken.None);

        Assert.Equal(1, hubFactory.LastCreated!.StartCallCount);
    }

    [Fact]
    public async Task StopAsync_DisposesConnection()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var sut = new OpenVpnProxyTrafficFlowClient(
            server,
            NullLogger<OpenVpnProxyTrafficFlowClient>.Instance,
            OpenVpnHubTestHelpers.CreateTrafficFlowHubMock(server.Id).Hub.Object,
            OpenVpnHubTestHelpers.CreateTokenService(),
            hubFactory);

        await sut.StartListeningAsync(CancellationToken.None);
        await sut.StopAsync();

        Assert.True(hubFactory.LastCreated!.Disposed);
    }

    [Fact]
    public async Task TrafficFlowUpdated_RelaysPayloadToFrontendHub()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy();
        var (hub, frontendClient) = OpenVpnHubTestHelpers.CreateTrafficFlowHubMock(server.Id);
        var sut = new OpenVpnProxyTrafficFlowClient(
            server,
            NullLogger<OpenVpnProxyTrafficFlowClient>.Instance,
            hub.Object,
            OpenVpnHubTestHelpers.CreateTokenService(),
            new SingleProxyHubConnectionFactory(proxy));

        await sut.StartListeningAsync(CancellationToken.None);
        await proxy.RaiseAsync("TrafficFlowUpdated", JToken.FromObject(new { serverId = server.Id, bytesIn = 10 }));

        frontendClient.Verify(
            c => c.SendCoreAsync(
                "TrafficFlowUpdated",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
