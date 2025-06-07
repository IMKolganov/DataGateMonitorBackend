namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;

public class AverageSessionDurationResponse
{
    public string ExternalId { get; set; } = null!;
    public string CommonName { get; set; } = null!;
    public string? TgUsername { get; set; }
    public string? TgFirstName { get; set; }
    public string? TgLastName { get; set; }
    public double AvgDurationMinutes { get; set; }
}
