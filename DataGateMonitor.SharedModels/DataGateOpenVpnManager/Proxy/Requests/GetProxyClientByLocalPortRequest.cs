namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy.Requests;

/// <summary>
/// Query for resolving a WebSocket client by the proxy's local endpoint toward OpenVPN.
/// </summary>
public class GetProxyClientByLocalPortRequest
{
    /// <summary>Ephemeral local port of the proxy socket (toward 127.0.0.1:vpn).</summary>
    public int LocalPort { get; set; }

    /// <summary>Host side of that local endpoint; usually loopback. Defaults to localhost.</summary>
    public string Host { get; set; } = "localhost";
}
