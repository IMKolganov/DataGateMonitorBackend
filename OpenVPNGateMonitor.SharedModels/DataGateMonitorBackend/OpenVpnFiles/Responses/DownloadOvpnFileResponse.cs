namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

public class DownloadOvpnFileResponse
{
    public int IssuedOvpnFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public byte[] Content { get; set; } = [];
}