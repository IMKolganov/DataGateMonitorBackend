using System.Collections.Generic;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using Newtonsoft.Json;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public class VpnServerWithStatusesV2Response
{
	public List<VpnServerWithStatusV2Dto> VpnServerWithStatuses { get; set; } = new List<VpnServerWithStatusV2Dto>();

	[JsonProperty("openVpnServerWithStatuses")]
	public List<VpnServerWithStatusV2Dto> OpenVpnServerWithStatuses => VpnServerWithStatuses;
}
