namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Dto;

public class QuotaPlanAllowedServerDto
{
    public int Id { get; set; }
    public int QuotaPlanId { get; set; }
    public int VpnServerId { get; set; }
}
