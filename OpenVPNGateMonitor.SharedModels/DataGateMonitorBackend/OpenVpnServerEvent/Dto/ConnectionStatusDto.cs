namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;

public class ConnectionStatusDto
{
    public int ServerId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    // public HubConnectionState State { get; set; }//todo: think about it
    public string? ConnectionId { get; set; } = string.Empty;
    public DateTimeOffset LastStateChangedUtc { get; set; }
    public DateTimeOffset? LastReconnectedUtc { get; set; }
    public DateTimeOffset? LastClosedUtc { get; set; }
    public string? LastError { get; set; } = string.Empty;
}