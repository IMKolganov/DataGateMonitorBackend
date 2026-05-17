using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.Services.BackgroundServices;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.Services.StatusStreamLogs;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.BackgroundServices;

public class StatusStreamLogCollectorBackgroundServiceTests
{
    [Fact]
    public async Task Collector_AppendsOnlyOnStatusSnapshotChanges()
    {
        var backgroundService = new Mock<IOpenVpnBackgroundService>();
        var store = new Mock<IStatusStreamLogStore>();
        var logger = new Mock<ILogger<StatusStreamLogCollectorBackgroundService>>();

        var call = 0;
        backgroundService.Setup(x => x.GetStatus()).Returns(() =>
        {
            call++;
            var status = call < 3 ? ServiceStatus.Idle : ServiceStatus.Running;
            return new Dictionary<int, ServiceStatusDto>
            {
                [1] = new()
                {
                    VpnServerId = 1,
                    Status = status
                }
            };
        });

        var appendCalls = 0;
        store.Setup(x => x.AppendAsync(It.IsAny<StatusStreamLogEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => appendCalls++);

        var sut = new StatusStreamLogCollectorBackgroundService(
            backgroundService.Object,
            store.Object,
            logger.Object);

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(1700);
        await sut.StopAsync(CancellationToken.None);

        Assert.Equal(2, appendCalls);
    }
}
