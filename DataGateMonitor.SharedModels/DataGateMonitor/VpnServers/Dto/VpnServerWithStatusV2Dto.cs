using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;
using Newtonsoft.Json;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

public class VpnServerWithStatusV2Dto
{
	public VpnServerV2Response VpnServerResponses { get; set; } = new VpnServerV2Response();

	[JsonProperty("openVpnServerResponses")]
	public VpnServerV2Response OpenVpnServerResponses => VpnServerResponses;

	public VpnServerStatusLogResponse? VpnServerStatusLogResponse { get; set; }

	public int CountConnectedClients { get; set; }

	public int CountSessions { get; set; }

	public long TotalBytesIn { get; set; }

	public long TotalBytesOut { get; set; }
}
