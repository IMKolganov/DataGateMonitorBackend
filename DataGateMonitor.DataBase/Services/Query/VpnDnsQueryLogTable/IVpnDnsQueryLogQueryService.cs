using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.VpnDnsQueryLogTable;

public interface IVpnDnsQueryLogQueryService
{
    Task<IPagedResult<VpnDnsQueryLog>> SearchAsync(
        GetVpnDnsQueryRequest request,
        CancellationToken ct,
        IReadOnlyList<string>? profileCommonNames = null);

    Task<IReadOnlyList<VpnDnsProfileSummaryItemDto>> GetProfileSummaryAsync(
        string externalId,
        IReadOnlyList<string> profileCommonNames,
        int vpnServerId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken ct);

    Task<(int TotalCount, DateTimeOffset? LastQueriedAtUtc)> GetServerSummaryAsync(int vpnServerId, CancellationToken ct);

    Task<IReadOnlyList<VpnDnsTopDomainDto>> GetTopDomainsAsync(GetVpnDnsTopDomainsRequest request, CancellationToken ct);
}
