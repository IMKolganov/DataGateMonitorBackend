using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;

public sealed class OverviewTotalsResponse
{
    public OverviewMetaDto Meta { get; set; } = new();
    public TotalsPayloadDto Totals { get; set; } = new();
}