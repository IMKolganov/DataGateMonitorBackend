namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;

public sealed class OverviewSeriesResponse
{
    public OverviewMeta Meta { get; set; } = new();
    public OverviewSummary Summary { get; set; } = new();
    public List<OverviewSeriesRow> Series { get; set; } = new();
}