namespace DataGateMonitor.Models.XrayNode;

public sealed class XrayNodeClientsResponse
{
    public List<XrayNodeClientDto> Clients { get; set; } = [];

    /// <summary>When omitted, the backend derives totals from <see cref="Clients"/>.</summary>
    public XrayNodeServerSnapshotDto? Server { get; set; }
}
