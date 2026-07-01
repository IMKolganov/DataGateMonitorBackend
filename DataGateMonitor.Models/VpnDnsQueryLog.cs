namespace DataGateMonitor.Models;

public class VpnDnsQueryLog : BaseEntity<int>
{
    public int VpnServerId { get; set; }

    public long PiHoleQueryId { get; set; }

    public int? UserId { get; set; }

    public string? ExternalId { get; set; }

    public string? CommonName { get; set; }

    public string ClientIp { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string? QueryType { get; set; }

    public string Status { get; set; } = string.Empty;

    private DateTimeOffset _queriedAtUtc;

    public DateTimeOffset QueriedAtUtc
    {
        get => _queriedAtUtc;
        set => _queriedAtUtc = value.ToUniversalTime();
    }
}
