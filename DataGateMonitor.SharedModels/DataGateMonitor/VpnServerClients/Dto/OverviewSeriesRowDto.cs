namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

public sealed class OverviewSeriesRowDto
{
    public DateTimeOffset Ts { get; set; }          // bucket start (UTC)
    public int ActiveClients { get; set; }          // average clients in bucket (rounded)
    public long TrafficInBytes { get; set; }        // sum in bucket
    public long TrafficOutBytes { get; set; }       // sum in bucket
    public long TrafficTotalBytes { get; set; }     // In + Out in bucket
}