using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Responses;

/// <summary>
/// Response model containing quota plan details.
/// </summary>
public class QuotaPlansResponse
{
    public List<QuotaPlanDto> QuotaPlans { get; set; } = new();
}
