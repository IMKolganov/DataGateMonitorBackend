namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Dto;

public class AverageSessionDurationDto
{
    public string ExternalId { get; set; } = null!;
    public string CommonName { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public double AvgDurationMinutes { get; set; }
}