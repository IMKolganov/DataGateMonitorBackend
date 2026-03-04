namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

public class OpenVpnServerStatusLogResponse
{
    public int VpnServerId { get; set; }
    public Guid SessionId { get; set; }
    public DateTimeOffset UpSince { get; set; }
    public string ServerLocalIp { get; set; } = string.Empty;
    public string ServerRemoteIp { get; set; } = string.Empty;
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
    public string Version { get; set; } = string.Empty;
}