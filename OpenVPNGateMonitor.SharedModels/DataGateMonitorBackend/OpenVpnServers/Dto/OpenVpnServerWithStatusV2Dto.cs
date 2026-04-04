using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

public class OpenVpnServerWithStatusV2Dto
{
    public OpenVpnServerV2Response OpenVpnServerResponses { get; set; } = new();
    public OpenVpnServerStatusLogResponse? OpenVpnServerStatusLogResponse { get; set; }
    public int CountConnectedClients { get; set; }
    public int CountSessions { get; set; }
    public long TotalBytesIn { get; set; }
    public long TotalBytesOut { get; set; }
}
