using System.Security.Claims;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api;

namespace DataGateMonitor.Controllers;

/// <summary>
/// Quota-plan allowlist filter for listing issued Xray links by ExternalId.
/// Privileged callers and users without an active plan see the full list (legacy Android / StatusGate).
/// </summary>
public static class XrayClientLinkQuotaFilter
{
    public static async Task<List<IssuedXrayClientLink>> FilterForCurrentUserAsync(
        ClaimsPrincipal user,
        IUserQuotaPlanQueryService userQuotaPlanQueryService,
        IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService,
        List<IssuedXrayClientLink> links,
        CancellationToken ct)
    {
        if (HttpUserContext.IsPrivileged(user))
            return links;
        if (!HttpUserContext.TryGetUserId(user, out var userId))
            return links;
        var uqp = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
        if (uqp is null)
            return links;
        var allowed = await quotaPlanAllowedServerQueryService.GetVpnServerIdsByQuotaPlanId(uqp.QuotaPlanId, ct);
        return links.Where(f => allowed.Contains(f.VpnServerId)).ToList();
    }
}
