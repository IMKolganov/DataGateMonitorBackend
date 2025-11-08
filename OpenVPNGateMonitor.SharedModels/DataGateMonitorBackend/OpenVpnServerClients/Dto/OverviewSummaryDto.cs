namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

public class OverviewSummaryDto
{
    public long TotalTrafficInBytes { get; set; }
    public long TotalTrafficOutBytes { get; set; }
    public int PeakActiveClients { get; set; }
}