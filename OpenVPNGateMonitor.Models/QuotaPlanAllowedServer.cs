namespace OpenVPNGateMonitor.Models;

public class QuotaPlanAllowedServer : BaseEntity<int>
{
    public int QuotaPlanId { get; set; }

    public int VpnServerId { get; set; }
}