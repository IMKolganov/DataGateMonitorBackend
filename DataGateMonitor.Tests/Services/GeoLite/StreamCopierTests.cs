using System.Text;
using Moq;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.GeoLite.Interfaces;

namespace DataGateMonitor.Tests.Services.GeoLite;

public class StreamCopierTests
{
    [Fact]
    public async Task CopyWithProgressAsync_Reports_Progress_And_Ends_At_100()
    {
        var notifier = new Mock<IGeoLiteProgressNotifier>();
        var sut = new StreamCopier(notifier.Object);

        var data = Encoding.UTF8.GetBytes(new string('x', 50_000));
        await using var src = new MemoryStream(data);
        await using var dst = new MemoryStream();

        await sut.CopyWithProgressAsync(src, dst, totalBytes: data.Length, currentStep: 4, totalSteps: 8,
            stepTitle: "Download file", CancellationToken.None);

        Assert.Equal(data.Length, dst.Length);
        notifier.Verify(n => n.ReportStepAsync(4, 8, "Download file", It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        notifier.Verify(n => n.ReportStepAsync(4, 8, "Download file", 100, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
