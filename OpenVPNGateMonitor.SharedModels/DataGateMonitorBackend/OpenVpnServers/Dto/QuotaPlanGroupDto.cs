namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

/// <summary>Quota plan a VPN server belongs to (for UI grouping).</summary>
public class QuotaPlanGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
