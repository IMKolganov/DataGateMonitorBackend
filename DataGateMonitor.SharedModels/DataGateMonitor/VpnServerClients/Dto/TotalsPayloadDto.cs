namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

public sealed class TotalsPayloadDto
{
    public long SessionsCount { get; set; }
    public long UsersCount { get; set; }
    public long TrafficInBytes { get; set; }
    public long TrafficOutBytes { get; set; }
    public long TrafficTotalBytes => TrafficInBytes + TrafficOutBytes;
}