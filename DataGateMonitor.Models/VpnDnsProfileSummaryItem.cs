namespace DataGateMonitor.Models;

public sealed class VpnDnsProfileSummaryItem
{
    public string CommonName { get; set; } = string.Empty;

    public int VpnServerId { get; set; }

    public bool IsRevoked { get; set; }

    public int QueryCount { get; set; }

    public DateTimeOffset? LastQueriedAtUtc { get; set; }
}
