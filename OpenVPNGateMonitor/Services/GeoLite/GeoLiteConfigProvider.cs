using OpenVPNGateMonitor.Services.GeoLite.Helpers;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;

namespace OpenVPNGateMonitor.Services.GeoLite;

public class GeoLiteConfigProvider(IServiceProvider sp) : IGeoLiteConfigProvider
{
    public async Task<string> GetDatabasePathAsync(CancellationToken ct)
    {
        // Reads "GeoIp_Db_Path" from settings
        var dbPath = await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Db_Path", sp, ct);
        return string.IsNullOrWhiteSpace(dbPath) ? 
            throw new InvalidOperationException("GeoIp_Db_Path is not configured.") : dbPath;
    }

    public string CreateTimestamp()
        => DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");

    public (string BaseDir, string ExtractDir, string TempFile) PreparePaths(string dbPath, string timestamp)
    {
        var baseDir = Path.GetDirectoryName(dbPath) ?? throw new InvalidOperationException("Invalid GeoIp_Db_Path");
        var extractDir = Path.Combine(baseDir, $"GeoLite2_{timestamp}");
        var tempFile = Path.Combine(extractDir, $"GeoLite2-City_{timestamp}.tar.gz");

        if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
        if (!Directory.Exists(extractDir)) Directory.CreateDirectory(extractDir);

        return (baseDir, extractDir, tempFile);
    }
}