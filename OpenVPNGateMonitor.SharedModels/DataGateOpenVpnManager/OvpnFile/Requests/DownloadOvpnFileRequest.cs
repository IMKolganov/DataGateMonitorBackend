namespace OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Requests;

public class DownloadOvpnFileRequest
{
    public string CommonName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}