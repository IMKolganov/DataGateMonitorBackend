using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Responses;

/// <summary>
/// Response model containing quota plan details.
/// </summary>
public class QuotaPlansResponse
{
    public List<QuotaPlanDto> QuotaPlans { get; set; } = new();
}
