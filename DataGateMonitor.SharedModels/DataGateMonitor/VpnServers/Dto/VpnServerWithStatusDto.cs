using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

public class VpnServerWithStatusDto
{
    public VpnServerResponse VpnServerResponses { get; set; } = new();
    public VpnServerStatusLogResponse? VpnServerStatusLogResponse { get; set; }
    public int CountConnectedClients  { get; set; }
    public int CountSessions { get; set; }
    public long TotalBytesIn { get; set; }
    public long TotalBytesOut { get; set; }
}