namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class StatusStreamLogEntryResponse
{
    public DateTimeOffset TimestampUtc { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public string Source { get; set; } = "memory";
}
