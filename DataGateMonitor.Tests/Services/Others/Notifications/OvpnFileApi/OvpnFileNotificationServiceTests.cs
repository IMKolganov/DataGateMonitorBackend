using FluentAssertions;
using Moq;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.Services.Others.Notifications.OvpnFileApi;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Others.Notifications.OvpnFileApi;

public class OvpnFileNotificationServiceTests
{
    private readonly Mock<INotificationService> _notifications = new(MockBehavior.Strict);
    private readonly OvpnFileNotificationService _sut;

    public OvpnFileNotificationServiceTests()
    {
        _sut = new OvpnFileNotificationService(_notifications.Object);
    }

    [Fact]
    public async Task NotifyReadAll_UsesWebAndTelegramChannels()
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

        await _sut.NotifyReadAll(7, 3, CancellationToken.None);

        channels.Should().BeEquivalentTo("web", "telegram");
        request!.PreferenceKind.Should().Be(ApplicationNotificationKind.OpenVpnProfileRead);
        request.Type.Should().Be("ovpn.read.all");
    }

    [Fact]
    public async Task NotifyIssued_UsesWebAndTelegramChannels()
    {
        IEnumerable<string>? channels = null;
        _notifications
            .Setup(s => s.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>?, CancellationToken>((_, ch, _) => channels = ch)
            .ReturnsAsync(1);

        await _sut.NotifyIssued(1, 10, "a.ovpn", "ext", CancellationToken.None);

        channels.Should().BeEquivalentTo("web", "telegram");
    }

    [Fact]
    public async Task NotifyReadAll_Xray_UsesWebAndTelegramChannels()
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

        await _sut.NotifyReadAll(9, 2, CancellationToken.None, VpnProfileNotificationStack.Xray);

        channels.Should().BeEquivalentTo("web", "telegram");
        request!.PreferenceKind.Should().Be(ApplicationNotificationKind.XrayProfileRead);
    }
}
