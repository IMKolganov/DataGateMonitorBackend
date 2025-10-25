namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Responses;

public class GeoLiteVersionCheckResponse
{
    public bool IsUpdateAvailable { get; set; }

    public DateTimeOffset? RemoteLastModified { get; set; }
    public string RemoteETag { get; set; } = string.Empty;
    public long? RemoteContentLength { get; set; }

    public DateTimeOffset? LocalLastWriteTimeUtc { get; set; }
    public long? LocalFileSize { get; set; }

    public string CheckedUrl { get; set; } = string.Empty;
}