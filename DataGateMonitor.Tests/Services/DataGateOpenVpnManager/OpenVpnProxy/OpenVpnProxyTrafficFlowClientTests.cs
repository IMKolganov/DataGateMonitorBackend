using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnProxyTrafficFlowClientTests
{
    [Fact]
    public async Task StartListeningAsync_WhenDisconnected_StartsConnection_AndRelaysFlowBatch()
    {
        var server = new VpnServer { Id = 15, ApiUrl = "https://node.example/api" };

        var groupProxy = new Mock<IClientProxy>();
        groupProxy
            .Setup(p => p.SendCoreAsync(
                "TrafficFlowUpdated",
                It.Is<object?[]>(args => args.Length == 1 && args[0] is JToken),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(c => c.Group(server.Id.ToString())).Returns(groupProxy.Object);

        var hubContext = new Mock<IHubContext<OpenVpnProxyTrafficFlowHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

        var tokenService = new Mock<IMicroserviceTokenService>();
        tokenService
            .Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("token");

        Func<JToken, Task>? capturedHandler = null;
        var connection = new Mock<IHubConnectionProxy>();
        connection.SetupSequence(c => c.State)
            .Returns(HubConnectionState.Disconnected)
            .Returns(HubConnectionState.Connected);
        connection.Setup(c => c.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        connection
            .Setup(c => c.On("TrafficFlowUpdated", It.IsAny<Func<JToken, Task>>()))
            .Callback<string, Func<JToken, Task>>((_, handler) => capturedHandler = handler);

        var hubFactory = new Mock<IHubConnectionFactory>();
        hubFactory
            .Setup(f => f.Create(
                "https://node.example/api/hubs/proxy-traffic-flow",
                It.IsAny<Func<Task<string?>>>()))
            .Returns(connection.Object)
            .Verifiable();

        var logger = new Mock<ILogger<OpenVpnProxyTrafficFlowClient>>();
        var client = new OpenVpnProxyTrafficFlowClient(
            server,
            logger.Object,
            hubContext.Object,
            tokenService.Object,
            hubFactory.Object);

        await client.StartListeningAsync(CancellationToken.None);

        capturedHandler.Should().NotBeNull();
        var payload = JArray.Parse("""[{ "connectionId": "c1" }]""");
        await capturedHandler!(payload);

        hubFactory.VerifyAll();
        connection.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        groupProxy.VerifyAll();
    }

    [Fact]
    public async Task StartListeningAsync_WhenAlreadyConnected_DoesNotRecreateConnection()
    {
        var server = new VpnServer { Id = 22, ApiUrl = "https://node.example" };

        var hubContext = new Mock<IHubContext<OpenVpnProxyTrafficFlowHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(Mock.Of<IHubClients>());

        var tokenService = new Mock<IMicroserviceTokenService>();
        tokenService
            .Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("token");

        var connection = new Mock<IHubConnectionProxy>();
        connection.SetupGet(c => c.State).Returns(HubConnectionState.Connected);
        connection.Setup(c => c.On("TrafficFlowUpdated", It.IsAny<Func<JToken, Task>>()));

        var hubFactory = new Mock<IHubConnectionFactory>();
        hubFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<Func<Task<string?>>>()))
            .Returns(connection.Object)
            .Verifiable();

        var logger = new Mock<ILogger<OpenVpnProxyTrafficFlowClient>>();
        var client = new OpenVpnProxyTrafficFlowClient(
            server,
            logger.Object,
            hubContext.Object,
            tokenService.Object,
            hubFactory.Object);

        await client.StartListeningAsync(CancellationToken.None);
        await client.StartListeningAsync(CancellationToken.None);

        hubFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<Func<Task<string?>>>()), Times.Once);
        connection.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartListeningAsync_RelaysManyPayloads_WithoutDropping()
    {
        const int payloadCount = 250;
        var server = new VpnServer { Id = 31, ApiUrl = "https://node.example" };

        var relayed = 0;
        var groupProxy = new Mock<IClientProxy>();
        groupProxy
            .Setup(p => p.SendCoreAsync(
                "TrafficFlowUpdated",
                It.Is<object?[]>(args => args.Length == 1 && args[0] is JToken),
                It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref relayed))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(c => c.Group(server.Id.ToString())).Returns(groupProxy.Object);

        var hubContext = new Mock<IHubContext<OpenVpnProxyTrafficFlowHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

        var tokenService = new Mock<IMicroserviceTokenService>();
        tokenService
            .Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("token");

        Func<JToken, Task>? capturedHandler = null;
        var connection = new Mock<IHubConnectionProxy>();
        connection.SetupSequence(c => c.State)
            .Returns(HubConnectionState.Disconnected)
            .Returns(HubConnectionState.Connected);
        connection.Setup(c => c.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        connection
            .Setup(c => c.On("TrafficFlowUpdated", It.IsAny<Func<JToken, Task>>()))
            .Callback<string, Func<JToken, Task>>((_, handler) => capturedHandler = handler);

        var hubFactory = new Mock<IHubConnectionFactory>();
        hubFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<Func<Task<string?>>>()))
            .Returns(connection.Object);

        var logger = new Mock<ILogger<OpenVpnProxyTrafficFlowClient>>();
        var client = new OpenVpnProxyTrafficFlowClient(
            server,
            logger.Object,
            hubContext.Object,
            tokenService.Object,
            hubFactory.Object);

        await client.StartListeningAsync(CancellationToken.None);
        capturedHandler.Should().NotBeNull();

        var payload = JArray.Parse("""[{ "connectionId": "c1", "clientToServerBytesDelta": 1 }]""");
        var tasks = Enumerable.Range(0, payloadCount).Select(_ => capturedHandler!(payload));
        await Task.WhenAll(tasks);

        relayed.Should().Be(payloadCount);
    }
}
