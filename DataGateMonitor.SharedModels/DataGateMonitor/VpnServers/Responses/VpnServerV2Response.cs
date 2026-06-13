using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using Newtonsoft.Json;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerV2Response
{
	public VpnServerV2Dto VpnServer { get; set; } = new VpnServerV2Dto();

	[JsonProperty("openVpnServer")]
	public VpnServerV2Dto OpenVpnServer => VpnServer;
}
