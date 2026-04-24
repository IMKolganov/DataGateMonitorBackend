using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Responses;

public class GetUserQuotaPlansByUserIdResponse
{
    public List<UserQuotaPlanDto> Items { get; set; } = new();
}
