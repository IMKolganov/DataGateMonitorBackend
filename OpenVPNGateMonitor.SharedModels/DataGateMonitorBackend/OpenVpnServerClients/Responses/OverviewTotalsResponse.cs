using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;

public sealed class OverviewTotalsResponse
{
    public OverviewMetaDto Meta { get; set; } = new();
    public TotalsPayloadDto Totals { get; set; } = new();
}