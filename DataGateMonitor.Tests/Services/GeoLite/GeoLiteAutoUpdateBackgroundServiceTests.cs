using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.Services.GeoLite;

namespace DataGateMonitor.Tests.Services.GeoLite;

public class GeoLiteAutoUpdateBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Invokes_ScheduledRunner_At_Least_Once()
    {
        var runner = new Mock<IGeoLiteScheduledUpdateRunner>();
        runner.Setup(r => r.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new GeoLiteAutoUpdateBackgroundService(
            NullLogger<GeoLiteAutoUpdateBackgroundService>.Instance,
            runner.Object);

        using var cts = new CancellationTokenSource();
        await sut.StartAsync(cts.Token);
        await Task.Delay(150, CancellationToken.None);
        cts.Cancel();

        try
        {
            await sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // some hosts propagate cancellation from StopAsync
        }

        runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
