using DataGateMonitor.SharedModels.DataGateMonitor.Applications.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Applications.Responses;

public class ApplicationResponse
{
    public ApplicationDto Application { get; set; } = new();
}