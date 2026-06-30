using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Responses;

public sealed class VpnDnsTopDomainsResponse
{
    public IReadOnlyList<VpnDnsTopDomainDto> Items { get; set; } = Array.Empty<VpnDnsTopDomainDto>();
}
