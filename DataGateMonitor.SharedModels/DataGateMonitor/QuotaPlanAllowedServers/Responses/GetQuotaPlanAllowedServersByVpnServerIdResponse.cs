using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlanAllowedServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlanAllowedServers.Responses;

public class GetQuotaPlanAllowedServersByVpnServerIdResponse
{
    public List<QuotaPlanAllowedServerDto> Items { get; set; } = new();
}
