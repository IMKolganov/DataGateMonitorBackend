using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Responses;

public class GetQuotaPlanAllowedServersByVpnServerIdResponse
{
    public List<QuotaPlanAllowedServerDto> Items { get; set; } = new();
}
