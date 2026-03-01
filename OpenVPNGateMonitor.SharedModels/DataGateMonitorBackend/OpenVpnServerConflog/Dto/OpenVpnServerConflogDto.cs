namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Dto;

public class OpenVpnServerConflogDto
{
    public int Id { get; set; }
    public int? VpnServerId { get; set; }
    public string RequestUrl { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreateDate { get; set; }
}
