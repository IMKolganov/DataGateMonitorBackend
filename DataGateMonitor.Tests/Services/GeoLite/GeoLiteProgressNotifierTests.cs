using Microsoft.AspNetCore.SignalR;
using Moq;
using DataGateMonitor.Hubs;
using DataGateMonitor.Services.GeoLite;

namespace DataGateMonitor.Tests.Services.GeoLite;

public class GeoLiteProgressNotifierTests
{
    [Fact]
    public async Task ReportStepAsync_Sends_To_Hub()
    {
        var proxy = new Mock<IClientProxy>();
        proxy.Setup(p => p.SendCoreAsync(
                It.Is<string>(m => m == "GeoLiteStepProgress"),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);

        var hub = new Mock<IHubContext<GeoLiteHub>>();
        hub.SetupGet(h => h.Clients).Returns(clients.Object);

        var notifier = new GeoLiteProgressNotifier(hub.Object);
        await notifier.ReportStepAsync(1, 8, "title", 42, CancellationToken.None);

        proxy.Verify(p => p.SendCoreAsync("GeoLiteStepProgress", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyFinishedAsync_Sends_To_Hub()
    {
        var proxy = new Mock<IClientProxy>();
        proxy.Setup(p => p.SendCoreAsync(
                It.Is<string>(m => m == "GeoLiteUpdateFinished"),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);

        var hub = new Mock<IHubContext<GeoLiteHub>>();
        hub.SetupGet(h => h.Clients).Returns(clients.Object);

        var notifier = new GeoLiteProgressNotifier(hub.Object);
        await notifier.NotifyFinishedAsync(CancellationToken.None);

        proxy.Verify(p => p.SendCoreAsync("GeoLiteUpdateFinished", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
