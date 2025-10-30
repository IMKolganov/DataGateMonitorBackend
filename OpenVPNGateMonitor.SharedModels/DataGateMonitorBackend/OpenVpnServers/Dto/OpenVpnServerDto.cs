namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

public class OpenVpnServerDto
{
    public int Id { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public string ApiUrl { get; set; } = string.Empty;
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
}