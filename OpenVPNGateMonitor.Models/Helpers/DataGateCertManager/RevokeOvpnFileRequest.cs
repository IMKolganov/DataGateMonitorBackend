namespace OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;

public class RevokeOvpnFileRequest
{
    public int VpnServerId {get; set;}
    public string CommonName { get; set; } = string.Empty;
    public string OvpnFileName { get; set; } = string.Empty;
    public string OvpnFilePath { get; set; } = string.Empty;
}