namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

public class GeoPointAggDto
{
    public string? Country = string.Empty;
    public string? Region = string.Empty;
    public double? Latitude;
    public double? Longitude;
    public int SessionsCount;
    public long TotalBytesIn;
    public long TotalBytesOut;
}