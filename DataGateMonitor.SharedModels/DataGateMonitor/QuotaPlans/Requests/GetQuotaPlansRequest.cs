namespace DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Requests;

/// <summary>
/// Request for retrieving quota plans (supports future filtering/paging).
/// </summary>
public class GetQuotaPlansRequest
{
    public bool IncludeInactive { get; set; } = false;
}