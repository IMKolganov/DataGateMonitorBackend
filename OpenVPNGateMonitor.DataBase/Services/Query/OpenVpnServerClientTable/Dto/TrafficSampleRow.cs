namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;


public sealed class TrafficSampleRow
{
    public Guid SessionId { get; set; }
    public DateTimeOffset MeasuredAt { get; set; }
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
}