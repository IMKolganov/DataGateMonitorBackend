using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.DataBase.Services.Query.VpnDnsQueryLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/vpn-dns-queries")]
[Authorize(Roles = "Admin")]
public class VpnDnsQueryController(
    IVpnDnsQueryLogQueryService queryService,
    IIssuedOvpnFileQueryService issuedOvpnFileQueryService) : BaseController
{
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<VpnDnsQueryPageResponse>>> Search(
        [FromQuery] GetVpnDnsQueryRequest request,
        [FromQuery] bool matchUserProfiles = false,
        CancellationToken cancellationToken = default)
    {
        var profileCommonNames = await ResolveProfileCommonNamesAsync(request.ExternalId, matchUserProfiles, cancellationToken);
        var page = await queryService.SearchAsync(request, cancellationToken, profileCommonNames);
        var response = new VpnDnsQueryPageResponse
        {
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            Items = page.Items.Adapt<List<DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto.VpnDnsQueryLogDto>>()
        };

        return Ok(ApiResponse<VpnDnsQueryPageResponse>.SuccessResponse(response));
    }

    [HttpGet("profile-summary")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VpnDnsProfileSummaryResponseItem>>>> ProfileSummary(
        [FromQuery] string externalId,
        [FromQuery] int vpnServerId = 0,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var ext = externalId?.Trim() ?? string.Empty;
        var issuedFiles = ext.Length == 0
            ? []
            : await issuedOvpnFileQueryService.GetAllByExternalId(ext, cancellationToken);

        var profileCommonNames = issuedFiles
            .Select(f => f.CommonName)
            .Where(cn => !string.IsNullOrWhiteSpace(cn))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var dnsByCn = await queryService.GetProfileSummaryAsync(
            ext,
            profileCommonNames,
            vpnServerId,
            fromUtc,
            toUtc,
            cancellationToken);

        var dnsLookup = dnsByCn.ToDictionary(
            x => ProfileKey(x.CommonName, x.VpnServerId),
            x => x,
            StringComparer.Ordinal);

        var items = issuedFiles
            .GroupBy(f => ProfileKey(f.CommonName, f.VpnServerId))
            .Select(g =>
            {
                var sample = g.First();
                dnsLookup.TryGetValue(ProfileKey(sample.CommonName, sample.VpnServerId), out var dns);
                return new VpnDnsProfileSummaryResponseItem
                {
                    CommonName = sample.CommonName,
                    VpnServerId = sample.VpnServerId,
                    IsRevoked = sample.IsRevoked,
                    QueryCount = dns?.QueryCount ?? 0,
                    LastQueriedAtUtc = dns?.LastQueriedAtUtc
                };
            })
            .OrderByDescending(x => x.QueryCount)
            .ThenBy(x => x.CommonName, StringComparer.Ordinal)
            .ToList();

        foreach (var dns in dnsByCn)
        {
            var key = ProfileKey(dns.CommonName, dns.VpnServerId);
            if (items.Any(x => ProfileKey(x.CommonName, x.VpnServerId) == key))
                continue;

            items.Add(new VpnDnsProfileSummaryResponseItem
            {
                CommonName = dns.CommonName,
                VpnServerId = dns.VpnServerId,
                IsRevoked = false,
                QueryCount = dns.QueryCount,
                LastQueriedAtUtc = dns.LastQueriedAtUtc
            });
        }

        return Ok(ApiResponse<IReadOnlyList<VpnDnsProfileSummaryResponseItem>>.SuccessResponse(items));
    }

    [HttpGet("top-domains")]
    public async Task<ActionResult<ApiResponse<VpnDnsTopDomainsResponse>>> TopDomains(
        [FromQuery] GetVpnDnsTopDomainsRequest request,
        CancellationToken cancellationToken)
    {
        var items = await queryService.GetTopDomainsAsync(request, cancellationToken);
        return Ok(ApiResponse<VpnDnsTopDomainsResponse>.SuccessResponse(new VpnDnsTopDomainsResponse
        {
            Items = items
        }));
    }

    private async Task<IReadOnlyList<string>?> ResolveProfileCommonNamesAsync(
        string? externalId,
        bool matchUserProfiles,
        CancellationToken cancellationToken)
    {
        if (!matchUserProfiles || string.IsNullOrWhiteSpace(externalId))
            return null;

        var files = await issuedOvpnFileQueryService.GetAllByExternalId(externalId.Trim(), cancellationToken);
        var names = files
            .Select(f => f.CommonName)
            .Where(cn => !string.IsNullOrWhiteSpace(cn))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return names.Count == 0 ? null : names;
    }

    private static string ProfileKey(string commonName, int vpnServerId) =>
        $"{vpnServerId}|{commonName}";
}

public sealed class VpnDnsProfileSummaryResponseItem
{
    public string CommonName { get; set; } = string.Empty;

    public int VpnServerId { get; set; }

    public bool IsRevoked { get; set; }

    public int QueryCount { get; set; }

    public DateTimeOffset? LastQueriedAtUtc { get; set; }
}
