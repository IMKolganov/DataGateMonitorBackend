using FluentAssertions;
using Moq;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.Services.Others.Notifications.CertApiClient;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Others.Notifications.CertApiClient;

public class CertificateNotificationServiceTests
{
    private readonly Mock<INotificationService> _notifications = new(MockBehavior.Strict);
    private readonly CertificateNotificationService _sut;

    public CertificateNotificationServiceTests()
    {
        _sut = new CertificateNotificationService(_notifications.Object);
    }

    [Fact]
    public async Task NotifyReadAllAsync_UsesWebAndTelegramChannels()
    {
        IEnumerable<string>? channels = null;
        NotifyAdminsRequest? request = null;
        _notifications
            .Setup(s => s.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>?, CancellationToken>((req, ch, _) =>
            {
                request = req;
                channels = ch;
            })
            .ReturnsAsync(1);

        await _sut.NotifyReadAllAsync(5, 4, CancellationToken.None);

        channels.Should().BeEquivalentTo("web", "telegram");
        request!.PreferenceKind.Should().Be(ApplicationNotificationKind.CertApiReadAll);
        request.Type.Should().Be("cert.read.all");
    }
}
