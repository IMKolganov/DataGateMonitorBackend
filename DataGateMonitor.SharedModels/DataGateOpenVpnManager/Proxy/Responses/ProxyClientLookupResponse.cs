using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy.Enums;

namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy.Responses;

/// <summary>
/// Result of <c>GET .../client/by-local-port</c>: active proxy session plus the host used for lookup.
/// </summary>
public class ProxyClientLookupResponse
{
    /// <summary>Host from the request (normalized for lookup); echoes the scope of the match.</summary>
    public string Host { get; set; } = string.Empty;

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
