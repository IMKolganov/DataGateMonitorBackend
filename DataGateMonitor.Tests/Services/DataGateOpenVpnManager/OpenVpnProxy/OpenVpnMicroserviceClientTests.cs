using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientTests
{
    private static OpenVpnMicroserviceClient CreateClient(
        VpnServer server,
        IHubConnectionFactory hubFactory,
        out Mock<IClientProxy> frontendClient,
        Mock<IOpenVpnMicroserviceNotificationService>? notifications = null)
    {
        var (hub, client) = OpenVpnHubTestHelpers.CreateFrontendHubMock(server.Id);
        frontendClient = client;
        return new OpenVpnMicroserviceClient(
            server,
            NullLogger<OpenVpnMicroserviceClient>.Instance,
            hub.Object,
            OpenVpnHubTestHelpers.CreateTokenService(),
            OpenVpnHubTestHelpers.CreateScopeFactory(notifications?.Object),
            hubFactory);
    }

    [Fact]
    public async Task SendCommandAsync_WhenDisconnected_StartsOnce()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var sut = CreateClient(server, hubFactory, out _);

        await sut.SendCommandAsync("status", CancellationToken.None);

        Assert.Equal(1, hubFactory.LastCreated!.StartCallCount);
        Assert.Equal(HubConnectionState.Connected, hubFactory.LastCreated.State);
    }

    [Fact]
    public async Task SendCommandAsync_WhenAlreadyConnected_DoesNotStartAgain()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var sut = CreateClient(server, hubFactory, out _);

        await sut.SendCommandAsync("status", CancellationToken.None);
        await sut.SendCommandAsync("status", CancellationToken.None);

        Assert.Equal(1, hubFactory.LastCreated!.StartCallCount);
    }

    [Fact]
    public async Task SendCommandAsync_WhenReconnecting_WaitsWithoutCallingStart()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var sut = CreateClient(server, hubFactory, out _);

        await sut.SendCommandAsync("ping", CancellationToken.None);
        var proxy = hubFactory.LastCreated!;
        Assert.Equal(1, proxy.StartCallCount);

        proxy.State = HubConnectionState.Reconnecting;
        _ = Task.Run(() => proxy.SimulateReconnectingThenConnectedAsync(TimeSpan.FromMilliseconds(50)));

        await sut.SendCommandAsync("status", CancellationToken.None);

        Assert.Equal(1, proxy.StartCallCount);
        Assert.Equal(HubConnectionState.Connected, proxy.State);
    }

    [Fact]
    public async Task ReconnectAsync_StopsThenStartsHub()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy { State = HubConnectionState.Disconnected };
        var sut = CreateClient(server, new SingleProxyHubConnectionFactory(proxy), out _);

        var mi = typeof(OpenVpnMicroserviceClient).GetMethod(
            "ReconnectAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var task = (Task)mi!.Invoke(sut, [proxy])!;
        await task;

        Assert.Equal(1, proxy.StopCallCount);
        Assert.Equal(1, proxy.StartCallCount);
        Assert.Equal(HubConnectionState.Connected, proxy.State);
    }

    [Fact]
    public async Task ReconnectAsync_OnFailure_NotifiesAndRethrows()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy
        {
            StartAsyncOverride = _ => throw new InvalidOperationException("reconnect failed")
        };
        var notifications = new Mock<IOpenVpnMicroserviceNotificationService>();
        notifications
            .Setup(x => x.NotifyReconnectFailed(server.Id, server.ServerName, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateClient(server, new SingleProxyHubConnectionFactory(proxy), out _, notifications);

        var mi = typeof(OpenVpnMicroserviceClient).GetMethod(
            "ReconnectAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        await Assert.ThrowsAsync<InvalidOperationException>(() => (Task)mi!.Invoke(sut, [proxy])!);
        notifications.Verify(
            x => x.NotifyReconnectFailed(server.Id, server.ServerName, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendCommandAsync_OnFailure_NotifiesAndBroadcastsError()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy
        {
            StartAsyncOverride = _ => throw new InvalidOperationException("hub down")
        };
        var notifications = new Mock<IOpenVpnMicroserviceNotificationService>();
        notifications
            .Setup(x => x.NotifySendCommandFailed(server.Id, server.ServerName, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateClient(server, new SingleProxyHubConnectionFactory(proxy), out var frontendClient, notifications);

        await sut.SendCommandAsync("status", CancellationToken.None);

        frontendClient.Verify(
            c => c.SendCoreAsync(
                "ReceiveCommandResult",
                It.Is<object?[]>(a => a.Length == 1 && a[0]!.ToString()!.Contains("hub down")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        notifications.Verify(
            x => x.NotifySendCommandFailed(server.Id, server.ServerName, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendCommandWithResponseAsync_ReturnsResultFromHandler()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy();
        proxy.InvokeTwoArgHandler = (method, arg1, _, _) =>
        {
            if (method == "SendCommandWithRequestId" && arg1 is string requestId)
                proxy.Raise("ReceiveCommandResultWithRequestId", requestId, "payload-42");
            return Task.CompletedTask;
        };

        var sut = CreateClient(server, new SingleProxyHubConnectionFactory(proxy), out _);
        var result = await sut.SendCommandWithResponseAsync("version", CancellationToken.None);

        Assert.Equal("payload-42", result);
        Assert.Equal(1, proxy.StartCallCount);
    }

    [Fact]
    public async Task ApiUrlChange_DisposesOldConnectionAndCreatesNew()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var hubFactory = new FakeHubConnectionFactory();
        var sut = CreateClient(server, hubFactory, out _);

        await sut.SendCommandAsync("status", CancellationToken.None);
        var first = hubFactory.Created[0];
        Assert.False(first.Disposed);

        server.ApiUrl = "https://new-host.datagateapp.com/";
        await sut.SendCommandAsync("status", CancellationToken.None);

        Assert.True(first.Disposed);
        Assert.Equal(2, hubFactory.Created.Count);
    }

    [Fact]
    public async Task SendCommandAsync_RestartsHub_AfterClosedEvent()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy();
        var sut = CreateClient(server, new SingleProxyHubConnectionFactory(proxy), out _);

        await sut.SendCommandAsync("ping", CancellationToken.None);
        Assert.Equal(1, proxy.StartCallCount);

        await proxy.RaiseClosedAsync(new InvalidOperationException("auto-reconnect exhausted"));

        await sut.SendCommandAsync("status", CancellationToken.None);
        Assert.Equal(2, proxy.StartCallCount);
    }

    [Fact]
    public async Task SendCommandToMicroserviceAsync_OnFailure_NotifiesAndBroadcastsError()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy
        {
            InvokeOneArgHandler = (_, _, _) => throw new InvalidOperationException("invoke failed")
        };
        var notifications = new Mock<IOpenVpnMicroserviceNotificationService>();
        notifications
            .Setup(x => x.NotifySendCommandFailed(server.Id, server.ServerName, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateClient(server, new SingleProxyHubConnectionFactory(proxy), out var frontendClient, notifications);

        await sut.SendCommandToMicroserviceAsync("status", CancellationToken.None);

        frontendClient.Verify(
            c => c.SendCoreAsync("ReceiveCommandResult", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        notifications.Verify(
            x => x.NotifySendCommandFailed(server.Id, server.ServerName, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendCommandWithResponseAsync_CancelledToken_CancelsTask()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy
        {
            InvokeTwoArgHandler = (_, _, _, _) => Task.CompletedTask
        };
        var sut = CreateClient(server, new SingleProxyHubConnectionFactory(proxy), out _);

        using var cts = new CancellationTokenSource();
        var task = sut.SendCommandWithResponseAsync("status", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task DisposeAsync_CancelsPendingCommandWaiters()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var proxy = new FakeHubConnectionProxy
        {
            InvokeTwoArgHandler = (method, _, _, _) =>
                method == "SendCommandWithRequestId" ? Task.CompletedTask : Task.CompletedTask
        };
        var sut = CreateClient(server, new SingleProxyHubConnectionFactory(proxy), out _);

        var responseTask = sut.SendCommandWithResponseAsync("slow", CancellationToken.None);
        await Task.Delay(50);
        await sut.DisposeAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);
    }
}
