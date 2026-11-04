namespace DataGateMonitor.Services.GeoLite.Interfaces;

public interface IGeoLiteVersionRetentionService
{
    Task<int> GetMaxKeptVersionsAsync(CancellationToken ct);

    /// <summary>
    /// Deletes oldest <c>GeoLite2_*</c> extract directories under the DB folder,
    /// keeping the newest <paramref name="maxKept"/> (or the configured default).
    /// </summary>
    /// <returns>Number of directories removed.</returns>
    Task<int> PruneOldVersionsAsync(string databasePath, CancellationToken ct, int? maxKept = null);

    /// <summary>Best-effort delete of an incomplete extract directory after a failed update.</summary>
    void TryDeleteDirectory(string? directoryPath);
}
