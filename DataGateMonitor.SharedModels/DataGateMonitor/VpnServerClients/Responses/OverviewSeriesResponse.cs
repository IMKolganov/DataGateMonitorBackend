using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

public sealed class OverviewSeriesResponse
{
    public OverviewMetaDto Meta { get; set; } = new();
    public OverviewSummaryDto Summary { get; set; } = new();
    public List<OverviewSeriesRowDto> OverviewSeriesRows { get; set; } = new();
}