using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Responses;

public sealed class VpnDnsQueryPageResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public IReadOnlyList<VpnDnsQueryLogDto> Items { get; set; } = Array.Empty<VpnDnsQueryLogDto>();
}
