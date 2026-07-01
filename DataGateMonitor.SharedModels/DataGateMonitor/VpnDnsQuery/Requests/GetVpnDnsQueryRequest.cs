namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;

public sealed class GetVpnDnsQueryRequest
{
    public int VpnServerId { get; set; }

    public string? ExternalId { get; set; }

    public string? CommonName { get; set; }

    public string? ClientIp { get; set; }

    public string? DomainContains { get; set; }

    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}
