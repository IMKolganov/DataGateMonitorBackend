using DataGateMonitor.Models;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.PiHoleHealth;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Notifications.Requests;
using Moq;

namespace DataGateMonitor.Tests.Services.PiHoleHealth;

public class PiHoleHealthNotificationServiceTests
{
    [Fact]
    public async Task NotifyUnhealthyAsync_UsesPiHoleNotificationType()
    {
        var notifications = new Mock<INotificationService>();
        NotifyAdminsRequest? captured = null;
        notifications.Setup(x => x.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(1);

        var sut = new PiHoleHealthNotificationService(notifications.Object);
        await sut.NotifyUnhealthyAsync(75, "Norway", "Warning", "Collector is disabled", CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(NotificationTypes.PiHoleCollectorUnhealthy, captured!.Type);
        Assert.Equal(ApplicationNotificationKind.OpenVpnServerSyncError, captured.PreferenceKind);
        Assert.Equal(NotificationSeverity.Warning, captured.Severity);
    }

    [Fact]
    public async Task NotifyRecoveredAsync_UsesPiHoleRecoveredNotificationType()
    {
        var notifications = new Mock<INotificationService>();
        NotifyAdminsRequest? captured = null;
        notifications.Setup(x => x.NotifyAdmins(It.IsAny<NotifyAdminsRequest>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<NotifyAdminsRequest, IEnumerable<string>, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(1);

        var sut = new PiHoleHealthNotificationService(notifications.Object);
        await sut.NotifyRecoveredAsync(75, "Norway", CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(NotificationTypes.PiHoleCollectorRecovered, captured!.Type);
        Assert.Equal(NotificationSeverity.Info, captured.Severity);
    }
}
