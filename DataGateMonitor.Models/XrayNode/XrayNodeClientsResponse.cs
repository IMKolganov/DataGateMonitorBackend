namespace DataGateMonitor.Models.XrayNode;

public sealed class XrayNodeClientsResponse
{
    public List<XrayNodeClientDto> Clients { get; set; } = [];

    /// <summary>When omitted, the backend derives totals from <see cref="Clients"/>.</summary>
    public XrayNodeServerSnapshotDto? Server { get; set; }

    /// <summary>Error reported by the Xray node when HTTP succeeded but stats query failed (optional).</summary>
    public string? PollError { get; set; }

    public DateTimeOffset? PolledAt { get; set; }
}
