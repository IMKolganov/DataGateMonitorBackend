using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Responses;

public class GetAllUserQuotaPlansResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<UserQuotaPlanDto> Items { get; set; } = new();
}
