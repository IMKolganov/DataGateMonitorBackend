namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto;

public sealed class VpnDnsProfileSummaryItemDto
{
    public string CommonName { get; set; } = string.Empty;

    public int VpnServerId { get; set; }

    public bool IsRevoked { get; set; }

    public int QueryCount { get; set; }

    public DateTimeOffset? LastQueriedAtUtc { get; set; }
}
