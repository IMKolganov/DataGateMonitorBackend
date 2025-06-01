namespace OpenVPNGateMonitor.Models;

public class OpenVpnServer : BaseEntity<int>
{
    public string ServerName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public string ApiUrl { get; set; } = string.Empty;
}