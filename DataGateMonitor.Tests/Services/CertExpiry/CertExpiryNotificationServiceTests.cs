using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications.CertExpiry;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Requests;
using Moq;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryNotificationServiceTests
{
    [Fact]
    public async Task NotifyExpiredAsync_UsesOvpnCertExpiredPreference()
    {
        var notifications = new Mock<INotificationService>();
        NotifyAdminsRequest? captured = null;
        notifications
            .Setup(n => n.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(1);

        var sut = new CertExpiryNotificationService(notifications.Object);
        var issued = new IssuedOvpnFile
        {
            Id = 42,
            VpnServerId = 3,
            CommonName = "user_1",
            ExternalId = "100"
        };

        await sut.NotifyExpiredAsync(
            issued,
            "TestServer",
            DateTimeOffset.UtcNow.AddDays(-1),
            "ABC123",
            CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(ApplicationNotificationKind.OvpnCertExpired, captured!.PreferenceKind);
        Assert.Equal(NotificationSeverity.Error, captured.Severity);
        Assert.Contains("user_1", captured.Message);
    }

    [Fact]
    public async Task NotifyExpiringSoonAsync_UsesWarningPreference()
    {
        var notifications = new Mock<INotificationService>();
        NotifyAdminsRequest? captured = null;
        notifications
            .Setup(n => n.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(1);

        var sut = new CertExpiryNotificationService(notifications.Object);
        var issued = new IssuedOvpnFile { Id = 1, VpnServerId = 2, CommonName = "cn", ExternalId = "ext" };

        await sut.NotifyExpiringSoonAsync(
            issued,
            "Srv",
            DateTimeOffset.UtcNow.AddDays(5),
            5,
            null,
            CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(ApplicationNotificationKind.OvpnCertExpiryWarning, captured!.PreferenceKind);
        Assert.Equal(NotificationSeverity.Warning, captured.Severity);
    }
}
