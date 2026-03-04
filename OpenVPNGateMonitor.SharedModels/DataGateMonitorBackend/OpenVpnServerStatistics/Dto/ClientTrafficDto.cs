namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Dto;

public class ClientTrafficDto
{
    public string ExternalId { get; set; } = null!;
    public string CommonName { get; set; } = null!;
    public string? TgUsername { get; set; }
    public string? TgFirstName { get; set; }
    public string? TgLastName { get; set; }
    public double TotalMbTraffic { get; set; }
}