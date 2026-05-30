namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public sealed class OverviewTrafficSample
{
    public int VpnServerId { get; init; }
    public Guid SessionId { get; init; }
    public string ExternalId { get; init; } = "";
    public DateTimeOffset MeasuredAt { get; init; }
    public long BytesIn { get; init; }
    public long BytesOut { get; init; }
}
