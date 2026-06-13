namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class StatusStreamLogsResponse
{
    public List<StatusStreamLogEntryResponse> Logs { get; set; } = [];
}
