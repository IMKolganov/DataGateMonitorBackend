using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy.Enums;

namespace OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy;

/// <summary>
/// Active WebSocket→OpenVPN proxy session (in-memory; used by DataGateOpenVpnManager and dashboard lookup by local port).
/// </summary>
public class ActiveProxyConnection
{
    public string ConnectionId { get; set; } = string.Empty;

    public ProxyConnectionProtocol Protocol { get; set; }

    public string? RealClientIp { get; set; }

    public int RealClientPort { get; set; }

    public string? LocalProxyIp { get; set; }

    public int LocalProxyPort { get; set; }

    public string? TargetIp { get; set; }

    public int TargetPort { get; set; }

    public DateTime ConnectedAtUtc { get; set; }
}
