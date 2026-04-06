using FluentAssertions;
using Moq;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.Services.Others.Notifications.GeoLite;
using OpenVPNGateMonitor.SharedModels.Enums;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Others.Notifications.GeoLite;

public class GeoLiteNotificationServiceTests
{
    private readonly Mock<INotificationService> _notificationService;
    private readonly GeoLiteNotificationService _sut;

    public GeoLiteNotificationServiceTests()
    {
        _notificationService = new Mock<INotificationService>(MockBehavior.Strict);
        _sut = new GeoLiteNotificationService(_notificationService.Object);
    }

    [Fact]
    public async Task NotifyAutoUpdateSucceededAsync_CallsNotifyAdmins_WithCorrectRequestAndChannels()
    {
        var path = "/data/geo-lite2-city.mmdb";
        var ct = CancellationToken.None;

        NotificationRequest? captured = null;
        IEnumerable<string>? capturedChannels = null;
        _notificationService
            .Setup(s => s.NotifyAdmins(It.IsAny<NotificationRequest>(), It.IsAny<IEnumerable<string>?>(), ct))
            .Callback<NotificationRequest, IEnumerable<string>?, CancellationToken>((req, ch, _) =>
            {
                captured = req;
                capturedChannels = ch;
            })
            .ReturnsAsync(1);

        await _sut.NotifyAutoUpdateSucceededAsync(path, ct);

        captured.Should().NotBeNull();
        captured!.Type.Should().Be(NotificationTypes.GeoLiteAutoUpdateSucceeded);
        captured.Title.Should().Be("GeoLite database updated");
        captured.Message.Should().Contain(path);
        captured.Severity.Should().Be(NotificationSeverity.Info);
        captured.Source.Should().Be("geolite-auto-update");
        capturedChannels.Should().BeEquivalentTo("web", "telegram");
    }

    [Fact]
    public async Task NotifyAutoUpdateFailedAsync_CallsNotifyAdmins_WithCorrectRequestAndChannels()
    {
        var ct = CancellationToken.None;

        NotificationRequest? captured = null;
        IEnumerable<string>? capturedChannels = null;
        _notificationService
            .Setup(s => s.NotifyAdmins(It.IsAny<NotificationRequest>(), It.IsAny<IEnumerable<string>?>(), ct))
            .Callback<NotificationRequest, IEnumerable<string>?, CancellationToken>((req, ch, _) =>
            {
                captured = req;
                capturedChannels = ch;
            })
            .ReturnsAsync(1);

        await _sut.NotifyAutoUpdateFailedAsync("download", "HttpRequestException: 503", ct);

        captured.Should().NotBeNull();
        captured!.Type.Should().Be(NotificationTypes.GeoLiteAutoUpdateFailed);
        captured.Title.Should().Be("GeoLite automatic update failed");
        captured.Message.Should().Contain("download").And.Contain("503");
        captured.Severity.Should().Be(NotificationSeverity.Error);
        captured.Source.Should().Be("geolite-auto-update");
        capturedChannels.Should().BeEquivalentTo("web", "telegram");
    }
}
