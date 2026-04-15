using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

public sealed class OverviewUsersSeriesResponse
{
    public OverviewMetaDto Meta { get; set; } = new();
    public OverviewUsersSeriesSummaryDto Summary { get; set; } = new();
    public List<OverviewUsersSeriesRowDto> Rows { get; set; } = new();
}
