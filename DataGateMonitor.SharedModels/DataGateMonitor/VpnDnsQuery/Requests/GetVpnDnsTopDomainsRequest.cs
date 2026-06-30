namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;

public sealed class GetVpnDnsTopDomainsRequest
{
    public int VpnServerId { get; set; }

    public string? ExternalId { get; set; }

    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }

    public int Limit { get; set; } = 100;
}
