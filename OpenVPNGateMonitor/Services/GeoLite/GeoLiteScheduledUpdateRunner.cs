using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Services.Others.Notifications.GeoLite;

namespace OpenVPNGateMonitor.Services.GeoLite;

public sealed class GeoLiteScheduledUpdateRunner(
    ILogger<GeoLiteScheduledUpdateRunner> logger,
    IServiceScopeFactory scopeFactory,
    IGeoLiteConfigProvider geoLiteConfig,
    IGeoLiteUpdaterService geoLiteUpdater)
    : IGeoLiteScheduledUpdateRunner
{
    public const string GeoIpAutoUpdateIntervalDays = "GeoIp_Auto_Update_Interval_Days";

    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await RunCoreAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GeoLite auto-update check failed");
            await NotifyFailedAsync("scheduled update", ex, ct).ConfigureAwait(false);
        }
    }

    private async Task RunCoreAsync(CancellationToken ct)
    {
        int intervalDays;
        using (var scope = scopeFactory.CreateScope())
        {
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            intervalDays = await settings.GetValueAsync<int>(GeoIpAutoUpdateIntervalDays, ct).ConfigureAwait(false);
        }

        if (intervalDays <= 0)
            return;

        string dbPath;
        try
        {
            dbPath = await geoLiteConfig.GetDatabasePathAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GeoLite auto-update: failed to resolve database path");
            await NotifyFailedAsync("configuration", ex, ct).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(dbPath))
        {
            await NotifyFailedAsync("configuration",
                "GeoIp_Db_Path is empty or invalid.", ct).ConfigureAwait(false);
            return;
        }

        var now = DateTime.UtcNow;
        if (File.Exists(dbPath))
        {
            var lastWrite = File.GetLastWriteTimeUtc(dbPath);
            if (lastWrite.AddDays(intervalDays) > now)
                return;
        }

        logger.LogInformation("GeoLite auto-update: interval elapsed ({IntervalDays} day(s)), checking remote version", intervalDays);

        try
        {
            var versionInfo = await geoLiteUpdater.CheckNewVersionAsync(ct).ConfigureAwait(false);
            if (!versionInfo.IsUpdateAvailable)
            {
                logger.LogInformation("GeoLite auto-update: local database is up to date");
                return;
            }

            logger.LogInformation("GeoLite auto-update: downloading new database");
            var updateResult = await geoLiteUpdater.DownloadAndUpdateDatabaseAsync(ct).ConfigureAwait(false);
            var path = updateResult.DatabasePath ?? dbPath;
            await NotifySucceededAsync(path, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GeoLite auto-update: version check or download failed");
            await NotifyFailedAsync("version check or download", ex, ct).ConfigureAwait(false);
        }
    }

    private async Task NotifySucceededAsync(string databasePath, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<IGeoLiteNotificationService>();
            await notifier.NotifyAutoUpdateSucceededAsync(databasePath, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send GeoLite auto-update success notification");
        }
    }

    private async Task NotifyFailedAsync(string phase, Exception ex, CancellationToken ct)
    {
        await NotifyFailedAsync(phase, $"{ex.GetType().Name}: {ex.Message}", ct).ConfigureAwait(false);
    }

    private async Task NotifyFailedAsync(string phase, string detail, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<IGeoLiteNotificationService>();
            await notifier.NotifyAutoUpdateFailedAsync(phase, detail, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send GeoLite auto-update failure notification");
        }
    }
}
