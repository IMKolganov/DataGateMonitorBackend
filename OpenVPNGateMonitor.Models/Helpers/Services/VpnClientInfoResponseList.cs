using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Models.Helpers.Services;

public class VpnClientInfoResponseList
{
    public List<VpnClientInfoResponse> VpnClientInfoResponse { get; set; } = new List<VpnClientInfoResponse>();
    public int TotalCount { get; set; }
}