namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

public class OverviewMetaDto
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public string Grouping { get; set; } = "";
    public string Timezone { get; set; } = "UTC";
    public string TrafficUnit { get; set; } = "bytes";
    public int? VpnServerId { get; set; }
}