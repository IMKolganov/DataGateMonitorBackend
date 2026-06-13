using DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.GeoLite.Responses;

public class GetGeoInfoResponse
{
    public OpenVpnGeoInfo GeoInfo { get; set; } = new();
}