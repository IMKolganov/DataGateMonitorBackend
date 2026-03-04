using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Responses;

/// <summary>
/// Response model containing quota plan details.
/// </summary>
public class QuotaPlanResponse
{
    public QuotaPlanDto QuotaPlan { get; set; } = new();
}
