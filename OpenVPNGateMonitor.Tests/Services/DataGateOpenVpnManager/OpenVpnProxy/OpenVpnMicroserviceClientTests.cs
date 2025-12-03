using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

namespace OpenVPNGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientTests
{
    private static (OpenVpnServer server,
        Mock<ILogger<OpenVpnMicroserviceClient>> log,
        Mock<IHubContext<OpenVpnFrontendHub>> hub,
        Mock<IHubClients> hubClients,
        Mock<IClientProxy> groupProxy,
        Mock<IMicroserviceTokenService> token,
        Mock<IHubConnectionFactory> factory,
        Mock<IHubConnectionProxy> connection) CreateCommon(string apiUrl = "https://ms.example")
    {
        var server = new OpenVpnServer { Id = 11, ApiUrl = apiUrl };
        var log = new Mock<ILogger<OpenVpnMicroserviceClient>>();
        var hub = new Mock<IHubContext<OpenVpnFrontendHub>>();
        var hubClients = new Mock<IHubClients>();
        var groupProxy = new Mock<IClientProxy>();
        hub.SetupGet(h => h.Clients).Returns(hubClients.Object);
        hubClients.Setup(c => c.Group(server.Id.ToString())).Returns(groupProxy.Object);
        groupProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var token = new Mock<IMicroserviceTokenService>();
        token.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .Returns("token");

        var factory = new Mock<IHubConnectionFactory>(MockBehavior.Strict);
        var connection = new Mock<IHubConnectionProxy>(MockBehavior.Strict);
        // default: not started yet, will be started in EnsureConnectionAsync
        HubConnectionState state = HubConnectionState.Disconnected;
        connection.SetupGet(c => c.State).Returns(() => state);
        connection.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
                  .Callback(() => state = HubConnectionState.Connected)
                  .Returns(Task.CompletedTask);
        connection.Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
                  .Callback(() => state = HubConnectionState.Disconnected)
                  .Returns(Task.CompletedTask);
        connection.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        // Handlers: capture, but not required by default
        connection.Setup(c => c.On<string>("ReceiveCommandResult", It.IsAny<Func<string, Task>>()))
                  .Verifiable();
        connection.Setup(c => c.On<string>("ReceiveMessage", It.IsAny<Func<string, Task>>()))
                  .Verifiable();
        connection.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()))
                  .Verifiable();

        factory.Setup(f => f.Create(It.Is<string>(u => u == $"{apiUrl}/hubs/openvpn"), It.IsAny<Func<Task<string?>>>() ))
               .Returns(connection.Object);

        return (server, log, hub, hubClients, groupProxy, token, factory, connection);
    }

    [Fact]
    public async Task SendCommandWithResponseAsync_Completes_When_Callback_Receives_Result()
    {
        var (server, log, hub, _, _, token, factory, connection) = CreateCommon();

        Action<string, string>? resultHandler = null;
        connection.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()))
                  .Callback<string, Action<string, string>>((_, h) => resultHandler = h)
                  .Verifiable();

        object?[]? sentArgs = null;
        connection.Setup(c => c.InvokeAsync("SendCommandWithRequestId", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                  .Callback<string, CancellationToken, object?[]>((_, __, args) => sentArgs = args)
                  .Returns(Task.CompletedTask);

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, factory.Object);

        var task = sut.SendCommandWithResponseAsync("status 3", CancellationToken.None);

        // Extract the generated requestId (args[0]) and simulate callback
        Assert.NotNull(sentArgs);
        var requestId = Assert.IsType<string>(sentArgs![0]);
        Assert.Equal("status 3", sentArgs![1]);

        Assert.NotNull(resultHandler);
        resultHandler!(requestId, "OK");

        var result = await task;
        Assert.Equal("OK", result);

        // Ensure connection start was called
        connection.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendCommandWithResponseAsync_Cancelled_Token_Cancels_Task_And_Cleans_Pending()
    {
        var (server, log, hub, _, _, token, factory, connection) = CreateCommon();
        connection.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()))
                  .Verifiable();
        connection.Setup(c => c.InvokeAsync("SendCommandWithRequestId", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                  .Returns(Task.CompletedTask);

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, factory.Object);

        using var cts = new CancellationTokenSource();
        var task = sut.SendCommandWithResponseAsync("status 3", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
    }

    [Fact]
    public async Task SendCommandAsync_Reconnects_When_NotConnected_Then_Sends()
    {
        var (server, log, hub, _, _, token, factory, connection) = CreateCommon();

        // Start disconnected; EnsureConnectionAsync will StartAsync and set Connected
        connection.Setup(c => c.InvokeAsync("SendCommand", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                  .Returns(Task.CompletedTask)
                  .Verifiable();

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, factory.Object);
        await sut.SendCommandAsync("ping", CancellationToken.None);

        // Started at least once and sent
        connection.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        connection.Verify(c => c.InvokeAsync("SendCommand", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()), Times.Once);
    }

    [Fact]
    public async Task SendCommandAsync_OnError_Logs_And_Forwards_Error_To_Group()
    {
        var (server, log, hub, hubClients, groupProxy, token, factory, connection) = CreateCommon();

        connection.Setup(c => c.InvokeAsync("SendCommand", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                  .ThrowsAsync(new InvalidOperationException("boom"));

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, factory.Object);
        await sut.SendCommandAsync("cmd", CancellationToken.None);

        groupProxy.Verify(p => p.SendCoreAsync(
                "ReceiveCommandResult",
                It.Is<object?[]>(arr => arr != null && arr.Length == 1 && arr[0] != null && arr[0]!.ToString()!.Contains("[Error] Failed to send command")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        log.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("Failed to send command to microservice")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UrlChange_DisposesOld_And_CreatesNew_Connection()
    {
        var (server, log, hub, _, _, token, factory, connection1) = CreateCommon("https://a.example");
        var connection2 = new Mock<IHubConnectionProxy>(MockBehavior.Strict);
        HubConnectionState state2 = HubConnectionState.Disconnected;
        connection2.SetupGet(c => c.State).Returns(() => state2);
        connection2.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
                   .Callback(() => state2 = HubConnectionState.Connected)
                   .Returns(Task.CompletedTask);
        connection2.Setup(c => c.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        connection2.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        connection2.Setup(c => c.On<string>(It.IsAny<string>(), It.IsAny<Func<string, Task>>()));
        connection2.Setup(c => c.On<string, string>(It.IsAny<string>(), It.IsAny<Action<string, string>>()));
        connection2.Setup(c => c.InvokeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                   .Returns(Task.CompletedTask);

        factory.Reset();
        factory.Setup(f => f.Create("https://a.example/hubs/openvpn", It.IsAny<Func<Task<string?>>>() ))
               .Returns(connection1.Object);
        factory.Setup(f => f.Create("https://b.example/hubs/openvpn", It.IsAny<Func<Task<string?>>>() ))
               .Returns(connection2.Object);

        // Prepare first connection handlers and basics
        HubConnectionState state1 = HubConnectionState.Disconnected;
        connection1.SetupGet(c => c.State).Returns(() => state1);
        connection1.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
                   .Callback(() => state1 = HubConnectionState.Connected)
                   .Returns(Task.CompletedTask);
        connection1.Setup(c => c.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        connection1.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask).Verifiable();
        connection1.Setup(c => c.On<string>(It.IsAny<string>(), It.IsAny<Func<string, Task>>()));
        connection1.Setup(c => c.On<string, string>(It.IsAny<string>(), It.IsAny<Action<string, string>>()));
        connection1.Setup(c => c.InvokeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                   .Returns(Task.CompletedTask);

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, factory.Object);

        // First call -> creates first connection
        await sut.SendCommandAsync("a", CancellationToken.None);

        // Change URL
        server.ApiUrl = "https://b.example";

        // Second call -> should dispose old and create new
        await sut.SendCommandAsync("b", CancellationToken.None);

        connection1.Verify(c => c.DisposeAsync(), Times.Once);
        factory.Verify(f => f.Create("https://a.example/hubs/openvpn", It.IsAny<Func<Task<string?>>>()), Times.Once);
        factory.Verify(f => f.Create("https://b.example/hubs/openvpn", It.IsAny<Func<Task<string?>>>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_Cancels_Pending_And_Disposes_Connection()
    {
        var (server, log, hub, _, _, token, factory, connection) = CreateCommon();

        Action<string, string>? resultHandler = null;
        connection.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()))
                  .Callback<string, Action<string, string>>((_, h) => resultHandler = h);
        object?[]? sentArgs = null;
        connection.Setup(c => c.InvokeAsync("SendCommandWithRequestId", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                  .Callback<string, CancellationToken, object?[]>((_, __, args) => sentArgs = args)
                  .Returns(Task.CompletedTask);

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, factory.Object);

        var cts = new CancellationTokenSource();
        var task = sut.SendCommandWithResponseAsync("status 3", cts.Token);

        // Dispose should cancel pending task
        await sut.DisposeAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);

        connection.Verify(c => c.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        connection.Verify(c => c.DisposeAsync(), Times.Once);
    }
}
