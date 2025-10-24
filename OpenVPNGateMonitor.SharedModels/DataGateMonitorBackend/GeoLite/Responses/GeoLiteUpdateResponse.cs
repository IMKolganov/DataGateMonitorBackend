namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Responses;

public class GeoLiteUpdateResponse
{
    public bool Success { get; set; } 
    public string? DownloadUrl { get; set; }
    public string? TempFilePath { get; set; } 
    public string? ExtractedPath { get; set; }
    public string? DatabasePath { get; set; }
    public string? ErrorMessage { get; set; }
}