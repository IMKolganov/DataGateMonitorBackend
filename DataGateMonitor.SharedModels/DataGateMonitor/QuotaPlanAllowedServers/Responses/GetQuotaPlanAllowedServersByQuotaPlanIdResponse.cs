using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlanAllowedServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlanAllowedServers.Responses;

public class GetQuotaPlanAllowedServersByQuotaPlanIdResponse
{
    public List<QuotaPlanAllowedServerDto> Items { get; set; } = new();
}
