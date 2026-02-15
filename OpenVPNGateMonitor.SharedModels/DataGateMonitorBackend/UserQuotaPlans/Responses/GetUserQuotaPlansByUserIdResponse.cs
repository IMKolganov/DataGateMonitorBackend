using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Responses;

public class GetUserQuotaPlansByUserIdResponse
{
    public List<UserQuotaPlanDto> Items { get; set; } = new();
}
