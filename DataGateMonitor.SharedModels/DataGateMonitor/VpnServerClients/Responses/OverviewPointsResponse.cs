using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

public class OverviewPointsResponse
{
    public List<GeoPointAggDto> GeoPointAggs = new();
}