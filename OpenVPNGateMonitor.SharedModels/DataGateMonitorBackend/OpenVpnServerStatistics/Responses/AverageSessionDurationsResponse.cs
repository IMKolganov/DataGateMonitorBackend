using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;

public class AverageSessionDurationsResponse
{
    public List<AverageSessionDurationDto> AverageSessionDurations { get; set; } = new();
}