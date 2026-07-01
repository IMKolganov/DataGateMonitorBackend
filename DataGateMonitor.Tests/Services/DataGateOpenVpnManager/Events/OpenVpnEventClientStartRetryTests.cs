using DataGateMonitor.Hubs;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.Events;

public class OpenVpnEventClientStartRetryTests
{
    [Fact]
    public async Task StartListeningAsync_OnStartFailure_RetriesWithInjectableDelay_AndNotifiesOnce()
    {
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        var failFirstStart = true;
        var proxy = new FakeHubConnectionProxy();
        proxy.StartAsyncOverride = _ =>
        {
            if (failFirstStart)
            {
                failFirstStart = false;
                throw new InvalidOperationException("hub unreachable");
            }

            proxy.State = HubConnectionState.Connected;
            return Task.CompletedTask;
        };

        var delayCalls = 0;
        var notifications = new Mock<IOpenVpnMicroserviceNotificationService>();
        notifications
            .Setup(x => x.NotifyEventHubConnectionFailed(
                server.Id, server.ServerName, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var scopeFactory = OpenVpnHubTestHelpers.CreateScopeFactory(notifications.Object);

        var client = new OpenVpnEventClient(
            server,
            NullLogger<OpenVpnEventClient>.Instance,
            Mock.Of<IHubContext<OpenVpnEventHub>>(),
            OpenVpnHubTestHelpers.CreateTokenService(),
            scopeFactory,
            new SingleProxyEventHubConnectionFactory(proxy),
            startRetryDelay: TimeSpan.FromMilliseconds(5),
            retryDelayAsync: (delay, ct) =>
            {
                delayCalls++;
                Assert.Equal(TimeSpan.FromMilliseconds(5), delay);
                return Task.CompletedTask;
            });

        await client.StartListeningAsync(CancellationToken.None);

        Assert.Equal(2, proxy.StartCallCount);
        Assert.Equal(1, delayCalls);
        Assert.Equal(HubConnectionState.Connected, proxy.State);
        notifications.Verify(
            x => x.NotifyEventHubConnectionFailed(
                server.Id, server.ServerName, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void OpenVpnHubConnectionDefaults_MicroserviceAndEventFactoriesShareReconnectSchedule()
    {
        Assert.Equal(6, OpenVpnHubConnectionDefaults.AutomaticReconnectDelays.Length);
        Assert.Equal(TimeSpan.Zero, OpenVpnHubConnectionDefaults.AutomaticReconnectDelays[0]);
        Assert.Equal(TimeSpan.FromSeconds(60), OpenVpnHubConnectionDefaults.AutomaticReconnectDelays[^1]);
        Assert.Equal(TimeSpan.FromSeconds(5), OpenVpnHubConnectionDefaults.StartFailureRetryDelay);
    }
}
