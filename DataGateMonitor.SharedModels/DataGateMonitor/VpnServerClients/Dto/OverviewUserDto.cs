namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

public sealed class OverviewUserDto
{
    public string? ExternalId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int? VpnServerId { get; set; } // null when mixed
    public int Sessions { get; set; }
    public long TrafficInBytes { get; set; }
    public long TrafficOutBytes { get; set; }
    public long TrafficTotalBytes => TrafficInBytes + TrafficOutBytes;
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
}