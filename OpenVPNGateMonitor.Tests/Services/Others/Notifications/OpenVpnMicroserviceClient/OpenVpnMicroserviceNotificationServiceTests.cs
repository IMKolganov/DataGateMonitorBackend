using FluentAssertions;
using Moq;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using OpenVPNGateMonitor.SharedModels.Enums;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Others.Notifications.OpenVpnMicroserviceClient;

public class OpenVpnMicroserviceNotificationServiceTests
{
    private readonly Mock<INotificationService> _notificationService;
    private readonly OpenVpnMicroserviceNotificationService _sut;

    public OpenVpnMicroserviceNotificationServiceTests()
    {
        _notificationService = new Mock<INotificationService>(MockBehavior.Strict);
        _sut = new OpenVpnMicroserviceNotificationService(_notificationService.Object);
    }

    [Fact]
    public async Task NotifySendCommandFailed_CallsNotifyAdmins_WithCorrectRequestAndChannels()
    {
        var serverId = 5;
        string? serverName = "vpn-server-1";
        string? errorMessage = "Connection refused";
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

        await _sut.NotifySendCommandFailed(serverId, serverName, errorMessage, ct);

        captured.Should().NotBeNull();
        captured!.Type.Should().Be("microservice.send-command-failed");
        captured.Title.Should().Be("Failed to send command to OpenVPN microservice");
        captured.Message.Should().Contain("ServerId=5").And.Contain("Name=vpn-server-1").And.Contain("Error=Connection refused");
        captured.Severity.Should().Be(NotificationSeverity.Error);
        captured.Source.Should().Be("openvpn-microservice-client");
        captured.ServerId.Should().Be(serverId);
        capturedChannels.Should().BeEquivalentTo("web", "telegram");
        _notificationService.VerifyAll();
    }

    [Fact]
    public async Task NotifySendCommandFailed_OmitsErrorInMessage_WhenErrorNullOrEmpty()
    {
        var serverId = 3;
        string? serverName = null;
        string? errorMessage = null;
        var ct = CancellationToken.None;

        NotificationRequest? captured = null;
        _notificationService
            .Setup(s => s.NotifyAdmins(It.IsAny<NotificationRequest>(), It.IsAny<IEnumerable<string>?>(), ct))
            .Callback<NotificationRequest, IEnumerable<string>?, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(2);

        await _sut.NotifySendCommandFailed(serverId, serverName, errorMessage, ct);

        captured.Should().NotBeNull();
        captured!.Message.Should().Be("ServerId=3");
        captured.Message.Should().NotContain("Error=");
        _notificationService.VerifyAll();
    }

    [Fact]
    public async Task NotifyReconnectFailed_CallsNotifyAdmins_WithCorrectRequestAndChannels()
    {
        var serverId = 7;
        string? serverName = "prod-vpn";
        string? errorMessage = "Timeout";
        var ct = CancellationToken.None;

        NotificationRequest? captured = null;
        _notificationService
            .Setup(s => s.NotifyAdmins(It.IsAny<NotificationRequest>(), It.IsAny<IEnumerable<string>?>(), ct))
            .Callback<NotificationRequest, IEnumerable<string>?, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(3);

        await _sut.NotifyReconnectFailed(serverId, serverName, errorMessage, ct);

        captured.Should().NotBeNull();
        captured!.Type.Should().Be("microservice.reconnect-failed");
        captured.Title.Should().Be("Failed to reconnect to OpenVPN microservice");
        captured.Message.Should().Contain("ServerId=7").And.Contain("Name=prod-vpn").And.Contain("Error=Timeout");
        captured.Severity.Should().Be(NotificationSeverity.Error);
        captured.Source.Should().Be("openvpn-microservice-client");
        captured.ServerId.Should().Be(serverId);
        _notificationService.VerifyAll();
    }

    [Fact]
    public async Task NotifyEventHubConnectionFailed_CallsNotifyAdmins_WithCorrectRequestAndChannels()
    {
        var serverId = 2;
        string? serverName = "event-hub-server";
        string? errorMessage = "401 Unauthorized";
        var ct = CancellationToken.None;

        NotificationRequest? captured = null;
        _notificationService
            .Setup(s => s.NotifyAdmins(It.IsAny<NotificationRequest>(), It.IsAny<IEnumerable<string>?>(), ct))
            .Callback<NotificationRequest, IEnumerable<string>?, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(4);

        await _sut.NotifyEventHubConnectionFailed(serverId, serverName, errorMessage, ct);

        captured.Should().NotBeNull();
        captured!.Type.Should().Be("microservice.event-hub-connection-failed");
        captured.Title.Should().Be("Failed to connect to OpenVPN microservice event hub");
        captured.Message.Should().Contain("ServerId=2").And.Contain("Name=event-hub-server").And.Contain("Error=401 Unauthorized");
        captured.Severity.Should().Be(NotificationSeverity.Error);
        captured.Source.Should().Be("openvpn-microservice-client");
        captured.ServerId.Should().Be(serverId);
        _notificationService.VerifyAll();
    }

    [Fact]
    public async Task NotifyProxyClientLookupFailed_CallsNotifyAdmins_WithCorrectTypeAndSeverity()
    {
        var serverId = 9;
        string? serverName = "proxy-test";
        var detail = "RealAddress=127.0.0.1:1; HTTP 404; localPort=1";
        var ct = CancellationToken.None;

        NotificationRequest? captured = null;
        _notificationService
            .Setup(s => s.NotifyAdmins(It.IsAny<NotificationRequest>(), It.IsAny<IEnumerable<string>?>(), ct))
            .Callback<NotificationRequest, IEnumerable<string>?, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(5);

        await _sut.NotifyProxyClientLookupFailed(serverId, serverName, detail, NotificationSeverity.Warning, ct);

        captured.Should().NotBeNull();
        captured!.Type.Should().Be("microservice.proxy-client-lookup-failed");
        captured.Title.Should().Be("Proxy client lookup failed");
        captured.Message.Should().Contain("ServerId=9").And.Contain("Name=proxy-test").And.Contain(detail);
        captured.Severity.Should().Be(NotificationSeverity.Warning);
        captured.Source.Should().Be("openvpn-microservice-client");
        captured.ServerId.Should().Be(serverId);
        _notificationService.VerifyAll();
    }
}
