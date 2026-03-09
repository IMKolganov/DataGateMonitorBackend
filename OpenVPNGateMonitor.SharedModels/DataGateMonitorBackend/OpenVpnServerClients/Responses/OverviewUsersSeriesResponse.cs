using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;

public sealed class OverviewUsersSeriesResponse
{
    public OverviewMetaDto Meta { get; set; } = new();
    public OverviewUsersSeriesSummaryDto Summary { get; set; } = new();
    public List<OverviewUsersSeriesRowDto> Rows { get; set; } = new();
}
