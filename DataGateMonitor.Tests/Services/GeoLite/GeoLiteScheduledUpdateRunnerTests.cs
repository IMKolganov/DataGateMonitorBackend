using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications.GeoLite;
using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Responses;
using Xunit;

namespace DataGateMonitor.Tests.Services.GeoLite;

public class GeoLiteScheduledUpdateRunnerTests
{
    private static IServiceScopeFactory CreateScopeFactory(
        ISettingsService settings,
        IGeoLiteNotificationService notifier)
    {
        var sc = new ServiceCollection();
        sc.AddScoped(_ => settings);
        sc.AddScoped(_ => notifier);
        var sp = sc.BuildServiceProvider();
        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task RunAsync_WhenIntervalIsZero_DoesNotTouchUpdater()
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var notifier = new Mock<IGeoLiteNotificationService>(MockBehavior.Strict);
        var config = new Mock<IGeoLiteConfigProvider>(MockBehavior.Strict);
        var updater = new Mock<IGeoLiteUpdaterService>(MockBehavior.Strict);

        var sut = new GeoLiteScheduledUpdateRunner(
            NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
            CreateScopeFactory(settings.Object, notifier.Object),
            config.Object,
            updater.Object);

        await sut.RunAsync(CancellationToken.None);

        updater.Verify(u => u.CheckNewVersionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenLocalFileStillFresh_SkipsVersionCheck()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GeoLiteTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "db.mmdb");
        await File.WriteAllTextAsync(dbPath, "x");
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddDays(-1));

        try
        {
            var settings = new Mock<ISettingsService>();
            settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>())).ReturnsAsync(7);
            var notifier = new Mock<IGeoLiteNotificationService>(MockBehavior.Strict);
            var config = new Mock<IGeoLiteConfigProvider>();
            config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbPath);
            var updater = new Mock<IGeoLiteUpdaterService>(MockBehavior.Strict);

            var sut = new GeoLiteScheduledUpdateRunner(
                NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
                CreateScopeFactory(settings.Object, notifier.Object),
                config.Object,
                updater.Object);

            await sut.RunAsync(CancellationToken.None);

            updater.Verify(u => u.CheckNewVersionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task RunAsync_WhenUpdateAvailable_DownloadsAndNotifiesSuccess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GeoLiteTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "db.mmdb");
        await File.WriteAllTextAsync(dbPath, "x");
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddDays(-10));

        try
        {
            var settings = new Mock<ISettingsService>();
            settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>())).ReturnsAsync(7);
            var notifier = new Mock<IGeoLiteNotificationService>();
            notifier.Setup(n => n.NotifyAutoUpdateSucceededAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var config = new Mock<IGeoLiteConfigProvider>();
            config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbPath);
            var updater = new Mock<IGeoLiteUpdaterService>();
            updater.Setup(u => u.CheckNewVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new GeoLiteVersionCheckResponse { IsUpdateAvailable = true });
            updater.Setup(u => u.DownloadAndUpdateDatabaseAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new GeoLiteUpdateResponse { Success = true, DatabasePath = dbPath });

            var sut = new GeoLiteScheduledUpdateRunner(
                NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
                CreateScopeFactory(settings.Object, notifier.Object),
                config.Object,
                updater.Object);

            await sut.RunAsync(CancellationToken.None);

            notifier.Verify(n => n.NotifyAutoUpdateSucceededAsync(dbPath, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task RunAsync_WhenCheckSaysNoUpdate_DoesNotDownloadOrNotifySuccess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GeoLiteTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "db.mmdb");
        await File.WriteAllTextAsync(dbPath, "x");
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddDays(-10));

        try
        {
            var settings = new Mock<ISettingsService>();
            settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>())).ReturnsAsync(7);
            var notifier = new Mock<IGeoLiteNotificationService>(MockBehavior.Strict);
            var config = new Mock<IGeoLiteConfigProvider>();
            config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbPath);
            var updater = new Mock<IGeoLiteUpdaterService>();
            updater.Setup(u => u.CheckNewVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new GeoLiteVersionCheckResponse { IsUpdateAvailable = false });

            var sut = new GeoLiteScheduledUpdateRunner(
                NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
                CreateScopeFactory(settings.Object, notifier.Object),
                config.Object,
                updater.Object);

            await sut.RunAsync(CancellationToken.None);

            updater.Verify(u => u.DownloadAndUpdateDatabaseAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task RunAsync_WhenDatabasePathThrows_NotifiesConfigurationFailure()
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var notifier = new Mock<IGeoLiteNotificationService>();
        notifier.Setup(n => n.NotifyAutoUpdateFailedAsync("configuration", It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var config = new Mock<IGeoLiteConfigProvider>();
        config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("bad path"));
        var updater = new Mock<IGeoLiteUpdaterService>(MockBehavior.Strict);

        var sut = new GeoLiteScheduledUpdateRunner(
            NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
            CreateScopeFactory(settings.Object, notifier.Object),
            config.Object,
            updater.Object);

        await sut.RunAsync(CancellationToken.None);

        notifier.Verify(
            n => n.NotifyAutoUpdateFailedAsync("configuration", It.Is<string>(m => m.Contains("InvalidOperationException", StringComparison.Ordinal)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenDbPathEmpty_NotifiesConfigurationFailure()
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var notifier = new Mock<IGeoLiteNotificationService>();
        notifier.Setup(n => n.NotifyAutoUpdateFailedAsync("configuration", It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var config = new Mock<IGeoLiteConfigProvider>();
        config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync("   ");
        var updater = new Mock<IGeoLiteUpdaterService>(MockBehavior.Strict);

        var sut = new GeoLiteScheduledUpdateRunner(
            NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
            CreateScopeFactory(settings.Object, notifier.Object),
            config.Object,
            updater.Object);

        await sut.RunAsync(CancellationToken.None);

        notifier.Verify(
            n => n.NotifyAutoUpdateFailedAsync("configuration", "GeoIp_Db_Path is empty or invalid.", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenLocalFileMissing_ChecksRemoteVersion()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GeoLiteTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "missing.mmdb");

        try
        {
            var settings = new Mock<ISettingsService>();
            settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>())).ReturnsAsync(7);
            var notifier = new Mock<IGeoLiteNotificationService>(MockBehavior.Strict);
            var config = new Mock<IGeoLiteConfigProvider>();
            config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbPath);
            var updater = new Mock<IGeoLiteUpdaterService>();
            updater.Setup(u => u.CheckNewVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new GeoLiteVersionCheckResponse { IsUpdateAvailable = false });

            var sut = new GeoLiteScheduledUpdateRunner(
                NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
                CreateScopeFactory(settings.Object, notifier.Object),
                config.Object,
                updater.Object);

            await sut.RunAsync(CancellationToken.None);

            updater.Verify(u => u.CheckNewVersionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task RunAsync_WhenDownloadFails_NotifiesFailure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GeoLiteTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "db.mmdb");
        await File.WriteAllTextAsync(dbPath, "x");
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddDays(-10));

        try
        {
            var settings = new Mock<ISettingsService>();
            settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>())).ReturnsAsync(7);
            var notifier = new Mock<IGeoLiteNotificationService>();
            notifier.Setup(n => n.NotifyAutoUpdateFailedAsync("version check or download", It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var config = new Mock<IGeoLiteConfigProvider>();
            config.Setup(c => c.GetDatabasePathAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbPath);
            var updater = new Mock<IGeoLiteUpdaterService>();
            updater.Setup(u => u.CheckNewVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new GeoLiteVersionCheckResponse { IsUpdateAvailable = true });
            updater.Setup(u => u.DownloadAndUpdateDatabaseAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("503"));

            var sut = new GeoLiteScheduledUpdateRunner(
                NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
                CreateScopeFactory(settings.Object, notifier.Object),
                config.Object,
                updater.Object);

            await sut.RunAsync(CancellationToken.None);

            notifier.Verify(
                n => n.NotifyAutoUpdateFailedAsync("version check or download", It.Is<string>(m => m.Contains("HttpRequestException", StringComparison.Ordinal)), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task RunAsync_WhenUnexpectedException_NotifiesScheduledUpdateFailure()
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetValueAsync<int>(GeoLiteScheduledUpdateRunner.GeoIpAutoUpdateIntervalDays, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("settings unavailable"));
        var notifier = new Mock<IGeoLiteNotificationService>();
        notifier.Setup(n => n.NotifyAutoUpdateFailedAsync("scheduled update", It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var config = new Mock<IGeoLiteConfigProvider>(MockBehavior.Strict);
        var updater = new Mock<IGeoLiteUpdaterService>(MockBehavior.Strict);

        var sut = new GeoLiteScheduledUpdateRunner(
            NullLogger<GeoLiteScheduledUpdateRunner>.Instance,
            CreateScopeFactory(settings.Object, notifier.Object),
            config.Object,
            updater.Object);

        await sut.RunAsync(CancellationToken.None);

        notifier.Verify(
            n => n.NotifyAutoUpdateFailedAsync("scheduled update", It.Is<string>(m => m.Contains("InvalidOperationException", StringComparison.Ordinal)), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
