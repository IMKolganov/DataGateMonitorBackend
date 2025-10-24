using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.GeoLite.Responses;

public class GetGeoInfoResponse
{
    public OpenVpnGeoInfo DatabasePath { get; set; } = new();
}