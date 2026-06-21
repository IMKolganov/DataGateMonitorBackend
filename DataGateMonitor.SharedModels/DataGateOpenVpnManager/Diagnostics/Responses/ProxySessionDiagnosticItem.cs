namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

/// <summary>
/// One active proxy session row in <c>GET api/diagnostics/proxy-sessions</c>.
/// </summary>
public sealed class ProxySessionDiagnosticItem
{
    public string ConnectionId { get; set; } = string.Empty;

    public string Protocol { get; set; } = string.Empty;

    public string RealClient { get; set; } = string.Empty;

    public string LocalProxy { get; set; } = string.Empty;

    public DateTime ConnectedAtUtc { get; set; }

    public long ProxyClientToServerBytes { get; set; }

    public long ProxyServerToClientBytes { get; set; }

    public bool InOpenVpnManagement { get; set; }

    public bool MissingFromManagement { get; set; }

    public bool IsZombie { get; set; }

    public string? OpenVpnCommonName { get; set; }

    public string? OpenVpnVirtualAddress { get; set; }

    public long? ManagementBytesReceived { get; set; }

    public long? ManagementBytesSent { get; set; }
}
