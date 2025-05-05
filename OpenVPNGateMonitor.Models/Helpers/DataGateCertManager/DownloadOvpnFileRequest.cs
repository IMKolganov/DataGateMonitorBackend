namespace OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;

public class DownloadOvpnFileRequest
{
    public string CommonName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}