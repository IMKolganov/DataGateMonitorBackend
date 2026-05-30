using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.Models.Helpers;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Models;
using DataGateMonitor.Services.Others.Notifications.TrafficDaily;
using DataGateMonitor.SharedModels.Enums;
using Moq;

namespace DataGateMonitor.Tests.Services.Others.Notifications.TrafficDaily;

public class TrafficDailyRollupNotificationServiceTests
{
    [Fact]
    public async Task NotifyCatchUpSucceededAsync_SendsInfoNotificationToAdmins()
    {
        var notifications = new Mock<INotificationService>();
        NotificationRequest? captured = null;
        IEnumerable<string>? channels = null;

        notifications
            .Setup(n => n.NotifyAdmins(It.IsAny<NotificationRequest>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationRequest, IEnumerable<string>?, CancellationToken>((req, ch, _) =>
            {
                captured = req;
                channels = ch;
            })
            .ReturnsAsync(1);

        var sut = new TrafficDailyRollupNotificationService(notifications.Object);
        var result = new TrafficDailyRollupCatchUpResult(
            [new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2)],
            42);

        await sut.NotifyCatchUpSucceededAsync(result, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(NotificationTypes.TrafficDailyRollupSucceeded, captured!.Type);
        Assert.Equal(NotificationSeverity.Info, captured.Severity);
        Assert.Null(captured.PreferenceKind);
        Assert.Contains("2 UTC day(s)", captured.Message, StringComparison.Ordinal);
        Assert.Contains("42 session-day row(s)", captured.Message, StringComparison.Ordinal);
        Assert.Equal(["web", "telegram"], channels);
    }

    [Fact]
    public async Task NotifyCatchUpFailedAsync_SendsErrorNotificationWithDayContext()
    {
        var notifications = new Mock<INotificationService>();
        NotificationRequest? captured = null;

        notifications
            .Setup(n => n.NotifyAdmins(It.IsAny<NotificationRequest>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationRequest, IEnumerable<string>?, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(1);

        var sut = new TrafficDailyRollupNotificationService(notifications.Object);
        var failure = new TrafficDailyRollupDayFailure(
            new DateOnly(2026, 5, 3),
            new InvalidOperationException("boom"),
            [new DateOnly(2026, 5, 1)],
            10);

        await sut.NotifyCatchUpFailedAsync(failure, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(NotificationTypes.TrafficDailyRollupFailed, captured!.Type);
        Assert.Equal(NotificationSeverity.Error, captured.Severity);
        Assert.Null(captured.PreferenceKind);
        Assert.Contains("2026-05-03", captured.Message, StringComparison.Ordinal);
        Assert.Contains("boom", captured.Message, StringComparison.Ordinal);
        Assert.Contains("Completed 1 day(s)", captured.Message, StringComparison.Ordinal);
    }
}
