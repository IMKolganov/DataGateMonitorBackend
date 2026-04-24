namespace DataGateMonitor.Services.GeoLite.Interfaces;

public interface IGeoLiteConfigProvider
{
    Task<string> GetDatabasePathAsync(CancellationToken ct);
    string CreateTimestamp();
    (string BaseDir, string ExtractDir, string TempFile) PreparePaths(string dbPath, string timestamp);
}