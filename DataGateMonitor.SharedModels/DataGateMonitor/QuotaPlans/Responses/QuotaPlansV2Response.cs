using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Responses;

/// <summary>v2 paged quota plans.</summary>
public class QuotaPlansV2Response
{
    public PagedResponse<QuotaPlanDto> QuotaPlans { get; set; } = new();
}
