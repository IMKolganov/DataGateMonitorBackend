using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Responses;

public sealed class VpnServerPiHoleConfigResponse
{
    public VpnServerPiHoleConfigDto Config { get; set; } = new();
}
