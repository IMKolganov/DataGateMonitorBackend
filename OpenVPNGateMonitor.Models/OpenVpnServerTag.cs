namespace OpenVPNGateMonitor.Models;

public class OpenVpnServerTag : BaseEntity<int>
{
    public int TagId { get; set; }
    public int VpnServerId { get; set; }
}
