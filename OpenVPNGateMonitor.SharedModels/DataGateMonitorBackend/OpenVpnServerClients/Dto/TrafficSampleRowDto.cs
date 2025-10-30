namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;


public sealed class TrafficSampleRowDto
{
    public Guid SessionId { get; set; }
    public DateTimeOffset MeasuredAt { get; set; }
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
}