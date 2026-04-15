using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Responses;

public class VpnServerConflogPageResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<VpnServerConflogDto> Items { get; set; } = [];
}
