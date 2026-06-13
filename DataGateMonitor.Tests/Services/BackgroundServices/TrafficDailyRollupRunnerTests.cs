using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.Services.BackgroundServices;
using DataGateMonitor.Services.Others.Notifications.TrafficDaily;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.BackgroundServices;

public class TrafficDailyRollupRunnerTests
{
    private static (TrafficDailyRollupRunner runner, Mock<IOverviewTrafficDailyRollupService> rollup, Mock<ITrafficDailyRollupNotificationService> notifier)
        CreateSut()
    {
        var rollup = new Mock<IOverviewTrafficDailyRollupService>();
        var notifier = new Mock<ITrafficDailyRollupNotificationService>();

        var services = new ServiceCollection();
        services.AddSingleton(rollup.Object);
        services.AddSingleton(notifier.Object);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        return (new TrafficDailyRollupRunner(scopeFactory, NullLogger<TrafficDailyRollupRunner>.Instance), rollup, notifier);
    }

    [Fact]
    public async Task RunCatchUpThroughYesterdayAsync_WhenUpToDate_DoesNotNotify()
    {
        var (sut, rollup, notifier) = CreateSut();
        rollup.Setup(r => r.GetMissingRollupDaysAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await sut.RunCatchUpThroughYesterdayAsync(CancellationToken.None);

        rollup.Verify(r => r.RollupDayAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(n => n.NotifyCatchUpSucceededAsync(It.IsAny<TrafficDailyRollupCatchUpResult>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(n => n.NotifyCatchUpFailedAsync(It.IsAny<TrafficDailyRollupDayFailure>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunCatchUpThroughYesterdayAsync_ProcessesMissingDays_AndNotifiesSuccess()
    {
        var (sut, rollup, notifier) = CreateSut();
        var day1 = new DateOnly(2026, 5, 1);
        var day2 = new DateOnly(2026, 5, 2);

        rollup.Setup(r => r.GetMissingRollupDaysAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([day1, day2]);
        rollup.Setup(r => r.RollupDayAsync(day1, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        rollup.Setup(r => r.RollupDayAsync(day2, It.IsAny<CancellationToken>())).ReturnsAsync(5);

        TrafficDailyRollupCatchUpResult? captured = null;
        notifier.Setup(n => n.NotifyCatchUpSucceededAsync(It.IsAny<TrafficDailyRollupCatchUpResult>(), It.IsAny<CancellationToken>()))
            .Callback<TrafficDailyRollupCatchUpResult, CancellationToken>((res, _) => captured = res)
            .Returns(Task.CompletedTask);

        await sut.RunCatchUpThroughYesterdayAsync(CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal([day1, day2], captured!.ProcessedDays);
        Assert.Equal(15, captured.SessionDayRowsUpserted);
    }

    [Fact]
    public async Task RunCatchUpThroughYesterdayAsync_OnDayFailure_NotifiesAndRethrows()
    {
        var (sut, rollup, notifier) = CreateSut();
        var day1 = new DateOnly(2026, 5, 1);
        var day2 = new DateOnly(2026, 5, 2);
        var boom = new InvalidOperationException("db down");

        rollup.Setup(r => r.GetMissingRollupDaysAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([day1, day2]);
        rollup.Setup(r => r.RollupDayAsync(day1, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        rollup.Setup(r => r.RollupDayAsync(day2, It.IsAny<CancellationToken>())).ThrowsAsync(boom);

        TrafficDailyRollupDayFailure? captured = null;
        notifier.Setup(n => n.NotifyCatchUpFailedAsync(It.IsAny<TrafficDailyRollupDayFailure>(), It.IsAny<CancellationToken>()))
            .Callback<TrafficDailyRollupDayFailure, CancellationToken>((f, _) => captured = f)
            .Returns(Task.CompletedTask);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RunCatchUpThroughYesterdayAsync(CancellationToken.None));

        Assert.Same(boom, ex);
        Assert.NotNull(captured);
        Assert.Equal(day2, captured!.DayUtc);
        Assert.Equal([day1], captured.CompletedDaysBeforeFailure);
        Assert.Equal(3, captured.SessionDayRowsUpsertedBeforeFailure);
    }

    [Fact]
    public async Task RunCatchUpThroughYesterdayAsync_WhenMissingLookupFails_NotifiesGenericFailure()
    {
        var (sut, rollup, notifier) = CreateSut();
        rollup.Setup(r => r.GetMissingRollupDaysAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("lookup timeout"));

        string? phase = null;
        notifier.Setup(n => n.NotifyCatchUpFailedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((p, _, _) => phase = p)
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<TimeoutException>(() => sut.RunCatchUpThroughYesterdayAsync(CancellationToken.None));

        Assert.Equal("missing-day lookup", phase);
    }
}
