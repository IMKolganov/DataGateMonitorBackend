namespace OpenVPNGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;

public class OpenVpnState
{
    public DateTimeOffset UpSince { get; set; }
    public bool Connected { get; set; }
    public bool Success { get; set; }
    public string ServerLocalIp { get; set; } = string.Empty;
    public string ServerRemoteIp { get; set; } = string.Empty;
}