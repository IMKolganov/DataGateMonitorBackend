using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientTests
{
    private static (VpnServer server,
        Mock<ILogger<OpenVpnMicroserviceClient>> log,
        Mock<IHubContext<OpenVpnFrontendHub>> hub,
        Mock<IHubClients> hubClients,
        Mock<IClientProxy> groupProxy,
        Mock<IMicroserviceTokenService> token,
        IServiceScopeFactory scopeFactory,
        Mock<IHubConnectionFactory> factory,
        Mock<IHubConnectionProxy> connection) CreateCommon(string apiUrl = "https://ms.example")
    {
        var server = new VpnServer { Id = 11, ApiUrl = apiUrl };
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

        HubConnectionState state = HubConnectionState.Disconnected;
        connection.SetupGet(c => c.State).Returns(() => state);
        connection.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
                  .Callback(() => state = HubConnectionState.Connected)
                  .Returns(Task.CompletedTask);
        connection.Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
                  .Callback(() => state = HubConnectionState.Disconnected)
                  .Returns(Task.CompletedTask);
        connection.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        connection.Setup(c => c.On<string>("ReceiveCommandResult", It.IsAny<Func<string, Task>>()))
                  .Verifiable();
        connection.Setup(c => c.On<string>("ReceiveMessage", It.IsAny<Func<string, Task>>()))
                  .Verifiable();
        connection.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()))
                  .Verifiable();

        factory.Setup(f => f.Create(It.Is<string>(u => u == $"{apiUrl}/hubs/openvpn"),
                                    It.IsAny<Func<Task<string?>>>()))
               .Returns(connection.Object);

        var microserviceNotification = new Mock<IOpenVpnMicroserviceNotificationService>();
        microserviceNotification.Setup(n => n.NotifySendCommandFailed(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                                .Returns(Task.CompletedTask);
        microserviceNotification.Setup(n => n.NotifyReconnectFailed(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                                .Returns(Task.CompletedTask);
        microserviceNotification.Setup(n => n.NotifyEventHubConnectionFailed(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                                .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton<IOpenVpnMicroserviceNotificationService>(microserviceNotification.Object);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        return (server, log, hub, hubClients, groupProxy, token, scopeFactory, factory, connection);
    }

    [Fact]
    public async Task SendCommandWithResponseAsync_Completes_When_Callback_Receives_Result()
    {
        var (server, log, hub, _, _, token, scopeFactory, factory, connection) = CreateCommon();

        Action<string, string>? resultHandler = null;
        connection.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()))
                  .Callback<string, Action<string, string>>((_, h) => resultHandler = h)
                  .Verifiable();

        string? capturedRequestId = null;
        string? capturedCommand = null;

        connection.Setup(c => c.InvokeAsync(
                            "SendCommandWithRequestId",
                            It.IsAny<object?>(),
                            It.IsAny<object?>(),
                            It.IsAny<CancellationToken>()))
                  .Callback<string, object?, object?, CancellationToken>((_, arg1, arg2, _) =>
                  {
                      capturedRequestId = Assert.IsType<string>(arg1);
                      capturedCommand = Assert.IsType<string>(arg2);
                  })
                  .Returns(Task.CompletedTask);

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, scopeFactory, factory.Object);

        var task = sut.SendCommandWithResponseAsync("status 3", CancellationToken.None);

        Assert.NotNull(capturedRequestId);
        Assert.Equal("status 3", capturedCommand);

        Assert.NotNull(resultHandler);
        resultHandler!(capturedRequestId!, "OK");

        var result = await task;
        Assert.Equal("OK", result);

        connection.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendCommandWithResponseAsync_Cancelled_Token_Cancels_Task_And_Cleans_Pending()
    {
        var (server, log, hub, _, _, token, scopeFactory, factory, connection) = CreateCommon();

        connection.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()))
                  .Verifiable();

        connection.Setup(c => c.InvokeAsync(
                            "SendCommandWithRequestId",
                            It.IsAny<object?>(),
                            It.IsAny<object?>(),
                            It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, scopeFactory, factory.Object);

        using var cts = new CancellationTokenSource();
        var task = sut.SendCommandWithResponseAsync("status 3", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
    }

    [Fact]
    public async Task SendCommandAsync_Reconnects_When_NotConnected_Then_Sends()
    {
        var (server, log, hub, _, _, token, scopeFactory, factory, connection) = CreateCommon();

        connection.Setup(c => c.InvokeAsync(
                            "SendCommand",
                            It.IsAny<object?>(),
                            It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask)
                  .Verifiable();

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, scopeFactory, factory.Object);
        await sut.SendCommandAsync("ping", CancellationToken.None);

        connection.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        connection.Verify(c => c.InvokeAsync(
                               "SendCommand",
                               It.IsAny<object?>(),
                               It.IsAny<CancellationToken>()),
                          Times.Once);
    }

    [Fact]
    public async Task SendCommandAsync_OnError_Logs_And_Forwards_Error_To_Group()
    {
        var (server, log, hub, hubClients, groupProxy, token, scopeFactory, factory, connection) = CreateCommon();

        connection.Setup(c => c.InvokeAsync(
                            "SendCommand",
                            It.IsAny<object?>(),
                            It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("boom"));

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, scopeFactory, factory.Object);
        await sut.SendCommandAsync("cmd", CancellationToken.None);

        groupProxy.Verify(p => p.SendCoreAsync(
                "ReceiveCommandResult",
                It.Is<object?[]>(arr =>
                    arr != null &&
                    arr.Length == 1 &&
                    arr[0] != null &&
                    arr[0]!.ToString()!.Contains("[Error] Failed to send command")),
                It.IsAny<CancellationToken>()),
            Times.Once);


        log.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state!.ToString()!.Contains("Failed to send command to microservice")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UrlChange_DisposesOld_And_CreatesNew_Connection()
    {
        var (server, log, hub, _, _, token, scopeFactory, factory, connection1) = CreateCommon("https://a.example");

        var connection2 = new Mock<IHubConnectionProxy>(MockBehavior.Strict);

        HubConnectionState state2 = HubConnectionState.Disconnected;
        connection2.SetupGet(c => c.State).Returns(() => state2);
        connection2.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
                   .Callback(() => state2 = HubConnectionState.Connected)
                   .Returns(Task.CompletedTask);
        connection2.Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
        connection2.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        connection2.Setup(c => c.On<string>(It.IsAny<string>(), It.IsAny<Func<string, Task>>()));
        connection2.Setup(c => c.On<string>("ReceiveMessage", It.IsAny<Func<string, Task>>()));
        connection2.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()));
        connection2.Setup(c => c.InvokeAsync(
                               It.IsAny<string>(),
                               It.IsAny<object?>(),
                               It.IsAny<object?>(),
                               It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        factory.Reset();
        factory.Setup(f => f.Create("https://a.example/hubs/openvpn", It.IsAny<Func<Task<string?>>>()))
               .Returns(connection1.Object);
        factory.Setup(f => f.Create("https://b.example/hubs/openvpn", It.IsAny<Func<Task<string?>>>()))
               .Returns(connection2.Object);

        HubConnectionState state1 = HubConnectionState.Disconnected;
        connection1.SetupGet(c => c.State).Returns(() => state1);
        connection1.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
                   .Callback(() => state1 = HubConnectionState.Connected)
                   .Returns(Task.CompletedTask);
        connection1.Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
        connection1.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask).Verifiable();
        connection1.Setup(c => c.On<string>(It.IsAny<string>(), It.IsAny<Func<string, Task>>()));
        connection1.Setup(c => c.On<string>("ReceiveMessage", It.IsAny<Func<string, Task>>()));
        connection1.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()));
        connection1.Setup(c => c.InvokeAsync(
                               It.IsAny<string>(),
                               It.IsAny<object?>(),
                               It.IsAny<object?>(),
                               It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, scopeFactory, factory.Object);

        await sut.SendCommandAsync("a", CancellationToken.None);

        server.ApiUrl = "https://b.example";

        await sut.SendCommandAsync("b", CancellationToken.None);

        connection1.Verify(c => c.DisposeAsync(), Times.Once);
        factory.Verify(f => f.Create("https://a.example/hubs/openvpn", It.IsAny<Func<Task<string?>>>()), Times.Once);
        factory.Verify(f => f.Create("https://b.example/hubs/openvpn", It.IsAny<Func<Task<string?>>>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_Cancels_Pending_And_Disposes_Connection()
    {
        var (server, log, hub, _, _, token, scopeFactory, factory, connection) = CreateCommon();

        Action<string, string>? resultHandler = null;
        connection.Setup(c => c.On<string, string>("ReceiveCommandResultWithRequestId", It.IsAny<Action<string, string>>()))
                  .Callback<string, Action<string, string>>((_, h) => resultHandler = h);

        string? capturedRequestId = null;
        string? capturedCommand = null;

        connection.Setup(c => c.InvokeAsync(
                            "SendCommandWithRequestId",
                            It.IsAny<object?>(),
                            It.IsAny<object?>(),
                            It.IsAny<CancellationToken>()))
                  .Callback<string, object?, object?, CancellationToken>((_, arg1, arg2, _) =>
                  {
                      capturedRequestId = Assert.IsType<string>(arg1);
                      capturedCommand = Assert.IsType<string>(arg2);
                  })
                  .Returns(Task.CompletedTask);

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, scopeFactory, factory.Object);

        var cts = new CancellationTokenSource();
        var task = sut.SendCommandWithResponseAsync("status 3", cts.Token);

        await sut.DisposeAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);

        connection.Verify(c => c.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        connection.Verify(c => c.DisposeAsync(), Times.Once);
    }
    
        [Fact]
    public async Task SendCommandToMicroserviceAsync_Reconnects_When_NotConnected_Then_Sends()
    {
        var (server, log, hub, _, _, token, scopeFactory, factory, connection) = CreateCommon();

        connection.Setup(c => c.InvokeAsync(
                            "SendCommand",
                            It.IsAny<object?>(),
                            It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask)
                  .Verifiable();

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, scopeFactory, factory.Object);

        await sut.SendCommandToMicroserviceAsync("ping", CancellationToken.None);

        connection.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        connection.Verify(c => c.InvokeAsync(
                               "SendCommand",
                               It.IsAny<object?>(),
                               It.IsAny<CancellationToken>()),
                          Times.Once);
    }

    [Fact]
    public async Task SendCommandToMicroserviceAsync_OnError_Logs_And_Forwards_Error_To_Group()
    {
        var (server, log, hub, _, groupProxy, token, scopeFactory, factory, connection) = CreateCommon();

        connection.Setup(c => c.InvokeAsync(
                            "SendCommand",
                            It.IsAny<object?>(),
                            It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("boom"));

        var sut = new OpenVpnMicroserviceClient(server, log.Object, hub.Object, token.Object, scopeFactory, factory.Object);

        await sut.SendCommandToMicroserviceAsync("cmd", CancellationToken.None);

        groupProxy.Verify(p => p.SendCoreAsync(
                "ReceiveCommandResult",
                It.Is<object?[]>(arr =>
                    arr != null &&
                    arr.Length == 1 &&
                    arr[0] != null &&
                    arr[0]!.ToString()!.Contains("[Error] Failed to send command to server")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        log.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state!.ToString()!.Contains("Failed to send command to microservice")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

}
