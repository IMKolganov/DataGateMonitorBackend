using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Responses;

public class AverageSessionDurationsResponse
{
    public List<AverageSessionDurationDto> AverageSessionDurations { get; set; } = new();
}