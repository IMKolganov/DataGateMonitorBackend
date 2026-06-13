using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Responses;

public class VpnServerConflogResponse
{
    public VpnServerConflogDto Item { get; set; } = new();
}
