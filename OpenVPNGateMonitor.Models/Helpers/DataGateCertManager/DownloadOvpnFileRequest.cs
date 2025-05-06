namespace OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;

public class DownloadOvpnFileRequest
{
    public int VpnServerId {get; set;}
    public string CommonName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}