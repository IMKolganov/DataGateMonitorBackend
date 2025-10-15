namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;

public sealed class OverviewTotalsResponse
{
    public OverviewMeta Meta { get; set; } = new();
    public TotalsPayload Totals { get; set; } = new();
}