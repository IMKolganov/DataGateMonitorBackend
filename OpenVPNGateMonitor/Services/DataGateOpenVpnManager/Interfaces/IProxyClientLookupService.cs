using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;

/// <summary>
/// Resolves the real WebSocket/proxy client when OpenVPN management reports a loopback <c>RealAddress</c>
/// (e.g. <c>127.0.0.1:41810</c>) via the DataGate OpenVPN microservice HTTP API.
/// </summary>
public interface IProxyClientLookupService
{
    /// <summary>
    /// When <see cref="OpenVpnServerClient.RemoteIp"/> is a loopback address (IP:port from management), calls the microservice and sets
    /// <see cref="OpenVpnServerClient.ProxyRealIp"/> plus Geo fields from the resolved public IP.
    /// Otherwise clears <see cref="OpenVpnServerClient.ProxyRealIp"/> and uses GeoLite on <see cref="OpenVpnServerClient.RemoteIp"/>.
    /// </summary>
    Task EnrichFromManagementRealAddressAsync(OpenVpnServer server, OpenVpnServerClient client, CancellationToken ct);
}
