namespace DataGateMonitor.Models.XrayNode;

/// <summary>Optional server-level stats from the Xray agent (JSON property <c>server</c>).</summary>
public sealed class XrayNodeServerSnapshotDto
{
    public DateTimeOffset? UpSince { get; set; }
    public string? Version { get; set; }
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
    public string? ServerLocalIp { get; set; }
}
