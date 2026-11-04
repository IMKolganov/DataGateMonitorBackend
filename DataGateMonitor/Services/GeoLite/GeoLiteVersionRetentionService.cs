using DataGateMonitor.Services.GeoLite.Interfaces;
using DataGateMonitor.Services.Others;
using Microsoft.Extensions.DependencyInjection;

namespace DataGateMonitor.Services.GeoLite;

public sealed class GeoLiteVersionRetentionService(
    IServiceScopeFactory scopeFactory,
    ILogger<GeoLiteVersionRetentionService> logger) : IGeoLiteVersionRetentionService
{
    public async Task<int> GetMaxKeptVersionsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var stored = await settings
            .GetValueAsync<int>(GeoLiteRetentionSettings.MaxKeptVersionsKey, ct)
            .ConfigureAwait(false);
        return GeoLiteRetentionSettings.ResolveMaxKept(stored);
    }

    public async Task<int> PruneOldVersionsAsync(string databasePath, CancellationToken ct, int? maxKept = null)
    {
        // Explicit 0 is allowed (pre-download: free a slot). Otherwise clamp via settings defaults.
        var keep = maxKept is null
            ? await GetMaxKeptVersionsAsync(ct).ConfigureAwait(false)
            : Math.Clamp(maxKept.Value, 0, GeoLiteRetentionSettings.MaxKeptVersionsCeiling);

        var baseDir = Path.GetDirectoryName(databasePath);
        if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir))
            return 0;

        var versionDirs = Directory.GetDirectories(baseDir, "GeoLite2_*")
            .Select(path => new DirectoryInfo(path))
            .OrderByDescending(d => d.Name, StringComparer.Ordinal)
            .ToList();

        if (versionDirs.Count <= keep)
            return 0;

        var removed = 0;
        foreach (var dir in versionDirs.Skip(keep))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                dir.Delete(recursive: true);
                removed++;
                logger.LogInformation("GeoLite retention: removed old version directory {Directory}", dir.FullName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GeoLite retention: failed to remove {Directory}", dir.FullName);
            }
        }

        if (removed > 0)
        {
            logger.LogInformation(
                "GeoLite retention: pruned {Removed} director(ies); keeping {Keep} newest under {BaseDir}",
                removed,
                keep,
                baseDir);
        }

        return removed;
    }

    public void TryDeleteDirectory(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            return;

        try
        {
            Directory.Delete(directoryPath, recursive: true);
            logger.LogInformation("GeoLite: removed incomplete extract directory {Directory}", directoryPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GeoLite: failed to remove incomplete extract directory {Directory}", directoryPath);
        }
    }
}
