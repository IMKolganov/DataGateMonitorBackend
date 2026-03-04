namespace OpenVPNGateMonitor.Models;

public class OpenVpnServerClientTraffic : BaseEntity<int>
{
    public int VpnServerId { get; set; }
    public int? UserId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public long BytesReceived { get; set; }
    public long BytesSent { get; set; }
    private DateTimeOffset _measuredAt;
    public DateTimeOffset MeasuredAt
    {
        get => _measuredAt;
        set => _measuredAt = value.ToUniversalTime();
    }
}