using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.Services.CertExpiry;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Invokes_ScheduledRunner_At_Least_Once()
    {
        var previous = Environment.GetEnvironmentVariable(CertExpiryEnvironment.DisabledVariable);
        try
        {
            Environment.SetEnvironmentVariable(CertExpiryEnvironment.DisabledVariable, null);

            var runner = new Mock<ICertExpiryScheduledCheckRunner>();
            runner.Setup(r => r.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var sut = new CertExpiryBackgroundService(
                NullLogger<CertExpiryBackgroundService>.Instance,
                runner.Object);

            using var cts = new CancellationTokenSource();
            await sut.StartAsync(cts.Token);
            await Task.Delay(1200, CancellationToken.None);
            cts.Cancel();

            try
            {
                await sut.StopAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
            }

            runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CertExpiryEnvironment.DisabledVariable, previous);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotInvokeRunner()
    {
        var key = CertExpiryEnvironment.DisabledVariable;
        var previous = Environment.GetEnvironmentVariable(key);
        var runner = new Mock<ICertExpiryScheduledCheckRunner>();

        try
        {
            Environment.SetEnvironmentVariable(key, "true");
            var sut = new CertExpiryBackgroundService(
                NullLogger<CertExpiryBackgroundService>.Instance,
                runner.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            await sut.StartAsync(cts.Token);
            await Task.Delay(80);
            await sut.StopAsync(CancellationToken.None);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }

        runner.Verify(r => r.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
