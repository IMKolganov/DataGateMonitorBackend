using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Responses;

public class GetAllQuotaPlanAllowedServersResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<QuotaPlanAllowedServerDto> Items { get; set; } = new();
}
