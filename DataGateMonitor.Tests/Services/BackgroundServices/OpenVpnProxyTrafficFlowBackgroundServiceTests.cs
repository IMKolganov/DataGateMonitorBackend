using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.BackgroundServices;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.BackgroundServices;

public class OpenVpnProxyTrafficFlowBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_StartsListeners_OnlyForEnabledOpenVpnServers()
    {
        var serverQuery = new Mock<IVpnServerQueryService>();
        serverQuery.Setup(q => q.GetAll(
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VpnServer>
            {
                new() { Id = 1, ServerType = VpnServerType.OpenVpn, IsDisable = false, ApiUrl = "https://ovpn-1" },
                new() { Id = 2, ServerType = VpnServerType.OpenVpn, IsDisable = true, ApiUrl = "https://ovpn-2" },
                new() { Id = 3, ServerType = VpnServerType.Xray, IsDisable = false, ApiUrl = "https://xray-1" }
            });

        var scopedProvider = new Mock<IServiceProvider>();
        scopedProvider.Setup(p => p.GetService(typeof(IVpnServerQueryService))).Returns(serverQuery.Object);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(scopedProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var flowClient = new Mock<IOpenVpnProxyTrafficFlowClient>();
        flowClient
            .Setup(c => c.StartListeningAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => started.TrySetResult(true));

        var factory = new Mock<IOpenVpnProxyTrafficFlowClientFactory>();
        factory.Setup(f => f.Create(It.Is<VpnServer>(s => s.Id == 1))).Returns(flowClient.Object);

        var supportChecker = new Mock<IOpenVpnProxyTrafficFlowSupportChecker>();
        supportChecker
            .Setup(c => c.ShouldListenAsync(It.Is<VpnServer>(s => s.Id == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        supportChecker
            .Setup(c => c.ShouldListenAsync(It.Is<VpnServer>(s => s.Id != 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logger = new Mock<ILogger<OpenVpnProxyTrafficFlowBackgroundService>>();
        var service = new OpenVpnProxyTrafficFlowBackgroundService(
            logger.Object,
            factory.Object,
            supportChecker.Object,
            scopeFactory.Object);

        await service.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(started.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        await service.StopAsync(CancellationToken.None);

        Assert.Same(started.Task, completed);
        factory.Verify(f => f.Create(It.Is<VpnServer>(s => s.Id == 1)), Times.AtLeastOnce);
        factory.Verify(f => f.Create(It.Is<VpnServer>(s => s.Id == 2)), Times.Never);
        factory.Verify(f => f.Create(It.Is<VpnServer>(s => s.Id == 3)), Times.Never);
        flowClient.Verify(c => c.StartListeningAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenServerUnsupported_RemovesClient_AndDoesNotStartListener()
    {
        var server = new VpnServer { Id = 4, ServerType = VpnServerType.OpenVpn, IsDisable = false, ApiUrl = "https://ovpn-old" };
        var serverQuery = new Mock<IVpnServerQueryService>();
        serverQuery.Setup(q => q.GetAll(
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VpnServer> { server });

        var scopedProvider = new Mock<IServiceProvider>();
        scopedProvider.Setup(p => p.GetService(typeof(IVpnServerQueryService))).Returns(serverQuery.Object);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(scopedProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var factory = new Mock<IOpenVpnProxyTrafficFlowClientFactory>();
        var supportChecker = new Mock<IOpenVpnProxyTrafficFlowSupportChecker>();
        supportChecker
            .Setup(c => c.ShouldListenAsync(server, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logger = new Mock<ILogger<OpenVpnProxyTrafficFlowBackgroundService>>();
        var service = new OpenVpnProxyTrafficFlowBackgroundService(
            logger.Object,
            factory.Object,
            supportChecker.Object,
            scopeFactory.Object);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        await service.StopAsync(CancellationToken.None);

        factory.Verify(f => f.Remove(server.Id), Times.AtLeastOnce);
        factory.Verify(f => f.Create(It.IsAny<VpnServer>()), Times.Never);
    }
}
