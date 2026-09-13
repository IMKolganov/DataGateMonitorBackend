using DataGateMonitor.SharedModels.DataGateMonitor.Applications.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Applications.Responses;

/// <summary>v2 paged applications.</summary>
public class ApplicationsV2Response
{
    public PagedResponse<ApplicationDto> Applications { get; set; } = new();
}
