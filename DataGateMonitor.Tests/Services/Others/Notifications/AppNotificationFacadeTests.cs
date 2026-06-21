using FluentAssertions;
using Moq;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Others.Notifications;

public class AppNotificationFacadeTests
{
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<INotificationCatalog> _catalog;
    private readonly AppNotificationFacade _sut;

    public AppNotificationFacadeTests()
    {
        _notificationService = new Mock<INotificationService>(MockBehavior.Strict);
        _catalog = new Mock<INotificationCatalog>(MockBehavior.Strict);
        _sut = new AppNotificationFacade(_notificationService.Object, _catalog.Object);
    }

    [Fact]
    public async Task SystemException_CallsCatalog_ThenNotifyAdmins_WithEnvelope()
    {
        var ex = new InvalidOperationException("Test error");
        var env = new NotificationEnvelope(
            new NotifyAdminsRequest { Type = "sys.ex", Title = "Error", Message = "InvalidOperationException: Test error", Severity = NotificationSeverity.Error },
            new[] { "web", "telegram" });
        var ct = CancellationToken.None;

        _catalog.Setup(c => c.SystemException(ex)).Returns(env);
        _notificationService
            .Setup(s => s.NotifyAdmins(env.Request, env.Channels, ct))
            .ReturnsAsync(1);

        await _sut.SystemException(ex, ct);

        _catalog.Verify(c => c.SystemException(ex), Times.Once);
        _notificationService.Verify(s => s.NotifyAdmins(env.Request, env.Channels, ct), Times.Once);
        _notificationService.VerifyAll();
        _catalog.VerifyAll();
    }

    [Fact]
    public async Task FileCreated_CallsCatalog_ThenNotifyAdmins()
    {
        var actorUserId = 10;
        var fileName = "config.ovpn";
        var env = new NotificationEnvelope(
            new NotifyAdminsRequest { Type = "file.created", Title = "New file", Message = $"User #{actorUserId} created file \"{fileName}\".", Severity = NotificationSeverity.Info },
            new[] { "web" });
        var ct = CancellationToken.None;

        _catalog.Setup(c => c.FileCreated(actorUserId, fileName)).Returns(env);
        _notificationService.Setup(s => s.NotifyAdmins(env.Request, env.Channels, ct)).ReturnsAsync(1);

        await _sut.FileCreated(actorUserId, fileName, ct);

        _catalog.Verify(c => c.FileCreated(actorUserId, fileName), Times.Once);
        _notificationService.Verify(s => s.NotifyAdmins(env.Request, env.Channels, ct), Times.Once);
        _notificationService.VerifyAll();
        _catalog.VerifyAll();
    }

    [Fact]
    public async Task CertIssued_CallsCatalog_ThenNotifyAdmins()
    {
        var actorUserId = 1;
        var commonName = "user@client";
        int? serverId = 5;
        var env = new NotificationEnvelope(
            new NotifyAdminsRequest { Type = "cert.issued", Title = "Certificate issued", Message = $"Certificate for \"{commonName}\" has been issued.", Severity = NotificationSeverity.Info, ServerId = serverId },
            new[] { "web", "telegram" });
        var ct = CancellationToken.None;

        _catalog.Setup(c => c.CertIssued(actorUserId, commonName, serverId)).Returns(env);
        _notificationService.Setup(s => s.NotifyAdmins(env.Request, env.Channels, ct)).ReturnsAsync(1);

        await _sut.CertIssued(actorUserId, commonName, serverId, ct);

        _catalog.Verify(c => c.CertIssued(actorUserId, commonName, serverId), Times.Once);
        _notificationService.Verify(s => s.NotifyAdmins(env.Request, env.Channels, ct), Times.Once);
        _notificationService.VerifyAll();
        _catalog.VerifyAll();
    }

    [Fact]
    public async Task ServerDown_CallsCatalog_ThenNotifyAdmins()
    {
        var serverId = 3;
        var serverName = "vpn-prod";
        var env = new NotificationEnvelope(
            new NotifyAdminsRequest { Type = "server.down", Title = "Server is DOWN", Message = $"VPN server \"{serverName}\" (id={serverId}) is unreachable.", Severity = NotificationSeverity.Critical, ServerId = serverId },
            new[] { "web", "telegram" });
        var ct = CancellationToken.None;

        _catalog.Setup(c => c.ServerDown(serverId, serverName)).Returns(env);
        _notificationService.Setup(s => s.NotifyAdmins(env.Request, env.Channels, ct)).ReturnsAsync(1);

        await _sut.ServerDown(serverId, serverName, ct);

        _catalog.Verify(c => c.ServerDown(serverId, serverName), Times.Once);
        _notificationService.Verify(s => s.NotifyAdmins(env.Request, env.Channels, ct), Times.Once);
        _notificationService.VerifyAll();
        _catalog.VerifyAll();
    }

    [Fact]
    public async Task ServerUp_CallsCatalog_ThenNotifyAdmins()
    {
        var serverId = 4;
        var serverName = "vpn-backup";
        var env = new NotificationEnvelope(
            new NotifyAdminsRequest { Type = "server.up", Title = "Server is UP", Message = $"VPN server \"{serverName}\" (id={serverId}) is reachable again.", Severity = NotificationSeverity.Info, ServerId = serverId },
            new[] { "web", "telegram" });
        var ct = CancellationToken.None;

        _catalog.Setup(c => c.ServerUp(serverId, serverName)).Returns(env);
        _notificationService.Setup(s => s.NotifyAdmins(env.Request, env.Channels, ct)).ReturnsAsync(1);

        await _sut.ServerUp(serverId, serverName, ct);

        _catalog.Verify(c => c.ServerUp(serverId, serverName), Times.Once);
        _notificationService.Verify(s => s.NotifyAdmins(env.Request, env.Channels, ct), Times.Once);
        _notificationService.VerifyAll();
        _catalog.VerifyAll();
    }
}
