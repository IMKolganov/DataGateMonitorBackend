using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Responses;

/// <summary>
/// Response model containing quota plan details.
/// </summary>
public class QuotaPlanResponse
{
    public QuotaPlanDto QuotaPlan { get; set; } = new();
}
