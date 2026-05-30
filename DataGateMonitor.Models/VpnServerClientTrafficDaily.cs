namespace DataGateMonitor.Models;

/// <summary>
/// Pre-aggregated traffic deltas for one session on one UTC calendar day.
/// </summary>
public class VpnServerClientTrafficDaily : BaseEntity<int>
{
    public int VpnServerId { get; set; }
    public int? UserId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public DateOnly DayUtc { get; set; }
    public long TrafficInBytes { get; set; }
    public long TrafficOutBytes { get; set; }
    public int SampleCount { get; set; }
}
