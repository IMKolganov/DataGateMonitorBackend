using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;

public sealed class OverviewSeriesResponse
{
    public OverviewMetaDto Meta { get; set; } = new();
    public OverviewSummaryDto Summary { get; set; } = new();
    public List<OverviewSeriesRowDto> OverviewSeriesRows { get; set; } = new();
}