namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Dto;

public class GeoConnectionDto
{
    public string Country { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string City { get; set; } = null!;
    public int Connections { get; set; }
}