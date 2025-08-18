namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;

public class OverviewSummary
{
    public long TotalTrafficInBytes { get; set; }
    public long TotalTrafficOutBytes { get; set; }
    public int PeakActiveClients { get; set; }
}