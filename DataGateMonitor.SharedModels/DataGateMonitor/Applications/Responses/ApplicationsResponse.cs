using DataGateMonitor.SharedModels.DataGateMonitor.Applications.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Applications.Responses;

public class ApplicationsResponse
{ 
    public List<ApplicationDto> Applications { get; set; } = new();
}