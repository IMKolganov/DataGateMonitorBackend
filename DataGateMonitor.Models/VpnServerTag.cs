namespace DataGateMonitor.Models;

public class VpnServerTag : BaseEntity<int>
{
    public int TagId { get; set; }
    public int VpnServerId { get; set; }
}
