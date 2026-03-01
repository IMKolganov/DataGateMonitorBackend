using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Responses;

public class OpenVpnServerConflogPageResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<OpenVpnServerConflogDto> Items { get; set; } = [];
}
