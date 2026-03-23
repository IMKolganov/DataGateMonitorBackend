namespace OpenVPNGateMonitor.Models;

public class OpenVpnServerClient : BaseEntity<int>
{
    public int VpnServerId { get; set; }
    public int? UserId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public string CommonName { get; set; } = string.Empty;
    public string RemoteIp { get; set; } = string.Empty;

    /// <summary>
    /// When <see cref="RemoteIp"/> is loopback (proxy toward OpenVPN), the real client IP:port from the
    /// microservice (<c>GET .../client/by-local-port</c>), e.g. <c>203.0.113.5:443</c>.
    /// </summary>
    public string? ProxyRealIp { get; set; }

    public string LocalIp { get; set; } = string.Empty;
    public long BytesReceived { get; set; }
    public long BytesSent { get; set; }
    private DateTimeOffset _connectedSince;
    public DateTimeOffset ConnectedSince
    {
        get => _connectedSince;
        set => _connectedSince = value.ToUniversalTime();
    }

    private DateTimeOffset? _disconnectedAt;
    public DateTimeOffset? DisconnectedAt
    {
        get => _disconnectedAt;
        set => _disconnectedAt = value?.ToUniversalTime();
    }
    public string Username { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsConnected { get; set; }
}